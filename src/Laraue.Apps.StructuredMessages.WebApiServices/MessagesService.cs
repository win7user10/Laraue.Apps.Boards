using Laraue.Apps.StructuredMessages.DataAccess;
using Laraue.Apps.StructuredMessages.DataAccess.Extensions;
using Laraue.Apps.StructuredMessages.DataAccess.Models;
using Laraue.Apps.StructuredMessages.Services;
using Laraue.Core.DataAccess.Contracts;
using Laraue.Core.DataAccess.EFCore.Extensions;
using Laraue.Core.DataAccess.Extensions;
using Laraue.Core.DateTime.Services.Abstractions;
using Laraue.Core.Exceptions.Web;
using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Laraue.Apps.StructuredMessages.WebApiServices;

public interface IMessagesService
{
    Task<BatchResult<MessageListDto>> GetMessages(
        GetMessagesRequest request,
        CancellationToken cancellationToken);
    
    Task<ColumnMessages[]> GetBoard(
        GetBoardRequest request,
        CancellationToken cancellationToken);
    
    Task<CategorySummary[]> GetBoardSummary(
        GetBoardSummaryRequest request,
        CancellationToken cancellationToken);

    Task UpdateStatus(
        UpdateStatusRequest request,
        CancellationToken ct);
    
    Task UpdateCategory(
        UpdateCategoryRequest request,
        CancellationToken ct);
    
    Task DeleteMessage(
        DeleteMessageRequest request,
        CancellationToken ct);
    
    Task<long> CreateMessage(
        CreateMessageRequest request,
        CancellationToken ct);
    
    Task EditMessage(
        EditMessageRequest request,
        CancellationToken ct);
    
    Task<IShortPaginatedResult<MessageListDto>> Search(
        SearchRequest request,
        CancellationToken ct);
    
    Task<MessageDetailDto> GetMessage(
        GetMessageRequest request,
        CancellationToken cancellationToken);
}

public class MessagesService(
    DatabaseContext context,
    ICoreMessageService messageService,
    ICoreCategoryService categoryService,
    IDateTimeProvider dateTimeProvider)
    : IMessagesService
{
    public async Task<BatchResult<MessageListDto>> GetMessages(
        GetMessagesRequest request,
        CancellationToken cancellationToken)
    {
        var statusId = request.StatusId == CoreMessageService.NullId
            ? null
            : request.StatusId;

        var query = context
            .Issues
            .Where(x => x.UserId == request.UserId)
            .Where(x => x.StatusId == statusId);
        
        if (!string.IsNullOrEmpty(request.SearchString))
            query = query
                .Where(x => EF.Functions.ILike(
                    x.Content!,
                    request.SearchString.AsSearchable()));

        var result = await ToBatchResult(ProjectToTemporaryDto(query
            .OrderByDescending(x => x.Id)), request);
        var projected = result.Data
            .Select(Map)
            .ToArray();
        
        await Enrich(projected, cancellationToken);

        return new BatchResult<MessageListDto>
        {
            HasNext = result.HasNext,
            Data = projected,
            Offset = result.Offset,
        };
    }

    private static async Task<BatchResult<T>> ToBatchResult<T>(
        IQueryable<T> queryable,
        BatchRequest request)
    {
        var requested = await queryable
            .Skip(request.Skip)
            .Take(request.Take + 1)
            .ToListAsyncEF();
        
        var hasNext = request.Take < requested.Count;
        var result = requested.Take(request.Take).ToArray();
        
        return new BatchResult<T>
        {
            HasNext = hasNext,
            Data = result,
            Offset = request.Skip + result.Length
        };
    }

    public async Task<ColumnMessages[]> GetBoard(
        GetBoardRequest request,
        CancellationToken cancellationToken)
    {
        var categoryId = request.CategoryId == CoreMessageService.NullId
            ? null
            : request.CategoryId;

        var statusIds = new List<long?>();
        if (categoryId is null)
            statusIds.Add(null);
        else
        {
            statusIds = await context.Statuses
                .Where(x => x.EpicId == categoryId.Value)
                .Where(x => x.Epic!.UserId == request.UserId)
                .Select(x => (long?)x.Id)
                .ToListAsyncEF(cancellationToken);
        }

        if (statusIds.Count == 0)
            throw new NotFoundException();

        var result = new List<ColumnMessages>();
        foreach (var statusId in statusIds)
        {
            var query = context
                .Issues
                .Where(x => x.UserId == request.UserId)
                .Where(x => x.StatusId == statusId);
            
            if (!string.IsNullOrEmpty(request.SearchString))
                query = query
                    .Where(x => EF.Functions.ILike(
                        x.Content!,
                        request.SearchString.AsSearchable()));
            
            var statusResult = await ProjectToTemporaryDto(query
                .OrderByDescending(x => x.Id))
                .FullPaginateEFAsync(
                    new PaginationData
                    {
                        Page = 0,
                        PerPage = request.Take,
                    },
                    cancellationToken);

            var mappedStatusResult = new InitialBatchResult<MessageListDto>()
            {
                Data = statusResult.Data.Select(Map).ToArray(),
                HasNext = statusResult.HasNextPage,
                Offset = request.Take,
                TotalCount = statusResult.Total,
            };
            
            result.Add(new ColumnMessages
            {
                StatusId = statusId ?? CoreMessageService.NullId,
                Items = mappedStatusResult,
            });
        }

        var allData = result
            .SelectMany(x => x.Items.Data)
            .ToList();
        
        await Enrich(allData, cancellationToken);

        return result.ToArray();
    }

    public async Task<CategorySummary[]> GetBoardSummary(
        GetBoardSummaryRequest request,
        CancellationToken cancellationToken)
    {
        var categoryById = (await context.Epics
            .Where(x => x.UserId == request.UserId)
            .Select(x => new
            {
                x.Id,
                x.Color,
                x.Name,
            })
            .ToArrayAsyncEF(cancellationToken))
            .ToDictionary(x => x.Id);
        
        var statusByCategoryId = (await context.Statuses
            .Where(x => categoryById.Keys.Contains(x.EpicId))
            .Select(x => new
            {
                x.Id,
                x.Color,
                x.Name,
                x.SortOrder,
                MessageCategoryId = x.EpicId,
            })
            .ToArrayAsyncEF(cancellationToken))
         .ToLookup(x => x.MessageCategoryId);
        
        var counts = (await context.Issues
            .Where(x => x.UserId == request.UserId)
            .Select(x => x)
            .GroupBy(x => x.StatusId)
            .Select(x => new
            {
                Id = x.Key ?? CoreMessageService.NullId,
                Count = x.Count(),
            })
            .ToArrayAsyncEF(cancellationToken))
            .ToDictionary(x => x.Id, x => x.Count);

        var result = categoryById
            .Select(category => new CategorySummary
            {
                Id = category.Key,
                Color = category.Value.Color,
                Name = category.Value.Name,
                Columns = statusByCategoryId[category.Key]
                    .Select(s => new ColumnSummary
                    {
                        Id = s.Id,
                        Color = s.Color,
                        Name = s.Name,
                        Count = counts.GetValueOrDefault(s.Id, 0),
                        SortOrder = s.SortOrder,
                    })
                    .OrderBy(s => s.SortOrder)
                    .ToArray()
            })
            .ToArray();

        return result;
    }

    public async Task UpdateStatus(UpdateStatusRequest request, CancellationToken ct)
    {
        if (!await messageService.UserHasAccessToMessage(request.UserId, request.MessageId, ct)) 
            throw new NotFoundException();
        
        await messageService.UpdateStatus(request.MessageId, request.StatusId, ct);
    }

    public async Task UpdateCategory(UpdateCategoryRequest request, CancellationToken ct)
    {
        if (!await messageService.UserHasAccessToMessage(request.UserId, request.MessageId, ct)) 
            throw new NotFoundException();
        
        await messageService.UpdateCategory(request.MessageId, request.CategoryId, ct);
    }

    public async Task DeleteMessage(DeleteMessageRequest request, CancellationToken ct)
    {
        if (!await messageService.UserHasAccessToMessage(request.UserId, request.MessageId, ct)) 
            throw new NotFoundException();

        await messageService.DeleteMessage(request.MessageId, ct);
    }

    public async Task<long> CreateMessage(CreateMessageRequest request, CancellationToken ct)
    {
        long? categoryId = request.CategoryId == CoreMessageService.NullId
            ? null
            : request.CategoryId;
        
        long? statusId = request.StatusId == CoreMessageService.NullId
            ? null
            : request.StatusId;

        if (categoryId.HasValue
            && !await categoryService.UserHasAccessToCategory(
                request.UserId, request.CategoryId, ct))
                    throw new BadRequestException(
                        nameof(categoryId),
                        "Category is not found");

        return await messageService.SaveMessage(
            new SaveMessageRequest
            {
                CreatedAt = dateTimeProvider.UtcNow,
                Text = request.Content,
                UserId = request.UserId,
                CategoryId = categoryId,
                StatusId = statusId,
            },
            ct);
    }

    public async Task EditMessage(EditMessageRequest request, CancellationToken ct)
    {
        if (!await messageService.UserHasAccessToMessage(request.UserId, request.MessageId, ct)) 
            throw new NotFoundException();
        
        await messageService.UpdateMessage(
            request.MessageId,
            upd => upd
                .SetProperty(x => x.Content, request.Content),
            ct);
    }

    public async Task<IShortPaginatedResult<MessageListDto>> Search(
        SearchRequest request,
        CancellationToken ct)
    {
        var query = context.Issues
            .Where(x => x.UserId == request.UserId);
        
        if (request.CategoryId.HasValue)
        {
            var categoryId = request.CategoryId == CoreMessageService.NullId
                ? null
                : request.CategoryId;

            query = query.Where(x => x.EpicId == categoryId);
        }
        
        if (!string.IsNullOrEmpty(request.SearchString))
            query = query
                .Where(x => EF.Functions.ILike(
                    x.Content!,
                    request.SearchString.AsSearchable()));

        var result = await ProjectToTemporaryDto(query)
            .ShortPaginateEFAsync(request, ct);

        var mapped = result.MapTo(Map);
        await Enrich(mapped.Data, ct);
        
        return mapped;
    }

    public async Task<MessageDetailDto> GetMessage(
        GetMessageRequest request,
        CancellationToken cancellationToken)
    {
        if (!await messageService.UserHasAccessToMessage(
            request.UserId, request.MessageId, cancellationToken))
            throw new NotFoundException();

        var result = await context.Issues
            .Where(x => x.Id == request.MessageId)
            .Select(x => new MessageDetailDtoData
            {
                Id = x.Id,
                Content = x.Content,
                Time = x.CreatedAt,
                CategoryName = x.Epic!.Name,
                StatusName = x.Status!.Name,
                TelegramFirstName = x.User!.TelegramFirstName,
                TelegramLastName = x.User!.TelegramLastName,
                TelegramId = x.User.TelegramId,
                TelegramUsername = x.User.TelegramUserName,
                CategoryColor = x.Epic.Color,
                StatusColor = x.Status.Color,
            })
            .FirstAsyncEF(cancellationToken);

        var sender = UserInitialsUtility.GetInitials(
            result.TelegramUsername,
            result.TelegramFirstName,
            result.TelegramLastName,
            result.TelegramId);;

        return new MessageDetailDto
        {
            Id = result.Id,
            Content = result.Content,
            Sender = sender.Sender,
            SenderInitial = sender.Initial,
            Time = result.Time,
            CategoryName = result.CategoryName,
            StatusName = result.StatusName,
            CategoryColor = result.CategoryColor,
            StatusColor = result.StatusColor,
        };
    }

    private async Task Enrich<T>(IList<T> elements, CancellationToken ct)
        where T : class, ICanContainMedia
    {
        var cardIds = elements.Select(x => x.Id).ToList();
        
        var directLinkedMessages = await context
            .TelegramMessages
            .Where(x => cardIds.Contains(x.Issue!.Id))
            .Select(x => new { x.Id, x.TelegramMediaGroupId, CardId = x.Issue!.Id })
            .ToListAsyncEF(ct);

        var groupIds = directLinkedMessages
            .Where(x => x.TelegramMediaGroupId.HasValue)
            .Select(x => x.TelegramMediaGroupId!.Value)
            .Distinct();

        var nonDirectLinkedMessages = await context
            .TelegramMessages
            .Where(x => groupIds.Contains(x.TelegramMediaGroupId!.Value))
            .Where(x => !directLinkedMessages.Select(y => y.Id).Contains(x.Id))
            .Select(x => new { x.Id, x.TelegramMediaGroupId })
            .ToArrayAsyncEF(ct);

        var allTelegramMessageIds = directLinkedMessages
            .Select(x => x.Id)
            .Union(nonDirectLinkedMessages.Select(y => y.Id))
            .ToArray();
        
        var photosData = await context.TelegramPhotos
            .Where(x => allTelegramMessageIds.Contains(x.TelegramMessageId))
            .Select(x => new
            {
                MessageId = x.TelegramMessageId,
                MessageGroupId = x.TelegramMessage!.TelegramMediaGroupId,
                x.TelegramFileId,
                x.PhotoType,
                x.GroupId,
            })
            .ToArrayAsyncEF(ct);

        var cardIdByTelegramMessageId = directLinkedMessages
            .ToDictionary(x => x.Id, x => x.CardId);
        var cardIdByMediaGroupId = directLinkedMessages
            .Where(x => x.TelegramMediaGroupId.HasValue)
            .ToDictionary(x => x.TelegramMediaGroupId!.Value, x => x.CardId);
        var photosByCardId = photosData
            .GroupBy(x =>
            {
                if (x.MessageGroupId is not null)
                    return cardIdByMediaGroupId[x.MessageGroupId.Value];
                return cardIdByTelegramMessageId[x.MessageId];
            })
            .ToDictionary(
                x => x.Key,
                x => x
                    .GroupBy(y => y.GroupId)
                    .ToDictionary(
                        y => y.Key,
                        y => y
                            .Select(z => new { z.PhotoType, z.TelegramFileId })));
        
        var videosData = await context.TelegramVideos
            .Where(x => allTelegramMessageIds.Contains(x.TelegramMessageId))
            .Select(x => new
            {
                MessageId = x.TelegramMessageId,
                MessageGroupId = x.TelegramMessage!.TelegramMediaGroupId,
                x.ThumbnailFileId,
                x.FileId
            })
            .ToArrayAsyncEF(ct);

        var videosByCardId = videosData
            .GroupBy(x =>
            {
                if (x.MessageGroupId is not null)
                    return cardIdByMediaGroupId[x.MessageGroupId.Value];
                return cardIdByTelegramMessageId[x.MessageId];
            })
            .ToDictionary(x => x.Key);
        
        foreach (var element in elements)
        {
            if (photosByCardId.TryGetValue(element.Id, out var photos))
                foreach (var photoGroup in photos)
                    element.Media.Add(new MediaInfo
                    {
                        Type = MediaType.Photo,
                        PreviewFileId = photoGroup.Value
                            .FirstOrDefault(x => x.PhotoType == PhotoType.Thumbnail)
                            ?.TelegramFileId,
                        OriginalFileId = photoGroup.Value
                            .FirstOrDefault(x => x.PhotoType == PhotoType.Original)
                            ?.TelegramFileId,
                    });
            
            if (videosByCardId.TryGetValue(element.Id, out var videos))
            {
                foreach (var video in videos)
                {
                    element.Media.Add(new MediaInfo
                    {
                        Type = MediaType.Video,
                        PreviewFileId = video.ThumbnailFileId,
                        OriginalFileId = video.FileId,
                    });
                }
            }
        }
    }

    private static IQueryable<MessageListDtoData> ProjectToTemporaryDto(
        IQueryable<Issue> queryable)
    {
        return queryable.Select(x => new MessageListDtoData
        {
            Id = x.Id,
            Content = x.Content,
            Time = x.CreatedAt,
            CategoryId = x.EpicId ?? CoreMessageService.NullId,
            StatusId = x.StatusId ?? CoreMessageService.NullId,
            TelegramFirstName = x.User!.TelegramFirstName,
            TelegramLastName = x.User!.TelegramLastName,
            TelegramId = x.User.TelegramId,
            TelegramUsername = x.User.TelegramUserName,
        });
    }
    
    private static MessageListDto Map(MessageListDtoData source)
    {
        var senderData = UserInitialsUtility.GetInitials(
            source.TelegramUsername,
            source.TelegramFirstName,
            source.TelegramLastName,
            source.TelegramId);

        return new MessageListDto
        {
            Id = source.Id,
            StatusId = source.StatusId,
            Content = source.Content,
            CategoryId = source.CategoryId,
            Sender = senderData.Sender,
            SenderInitial = senderData.Initial,
            Time = source.Time
        };
    }
}

public record UpdateStatusRequest
{
    public Guid UserId { get; set; }
    public long MessageId { get; set; }
    public long StatusId { get; set; }
}

public record UpdateCategoryRequest
{
    public Guid UserId { get; set; }
    public long MessageId { get; set; }
    public long CategoryId { get; set; }
}

public record GetMessagesRequest : BatchRequest
{
    public Guid UserId { get; set; }
    public long? StatusId { get; set; }
    public string? SearchString { get; set; }
}

public record GetMessageRequest
{
    public Guid UserId { get; set; }
    public long MessageId { get; set; }
}

public record GetBoardRequest
{
    public Guid UserId { get; set; }
    public long? CategoryId { get; set; }
    public int Take { get; init; }
    public string? SearchString { get; init; }
}

public record GetBoardSummaryRequest
{
    public Guid UserId { get; set; }
}

public record ColumnMessages
{
    public required long StatusId { get; set; }
    public required InitialBatchResult<MessageListDto> Items { get; set; }
}

public class MessageListDtoData
{
    public required long Id { get; set; }
    public required DateTime Time { get; set; }
    public required long TelegramId { get; set; }
    public required string? TelegramUsername { get; set; }
    public required string? TelegramFirstName { get; set; }
    public required string? TelegramLastName { get; set; }
    public required string? Content { get; set; }
    public required long CategoryId { get; set; }
    public required long StatusId { get; set; }
}

public interface ICanContainMedia
{
    public long Id { get; set; }
    public List<MediaInfo> Media { get; set; }
}

public class MessageListDto : ICanContainMedia
{
    public required long Id { get; set; }
    public required DateTime Time { get; set; }
    public required string? Sender { get; set; }
    public string? SenderInitial { get; set; }
    public required string? Content { get; set; }
    public required long CategoryId { get; set; }
    public required long StatusId { get; set; }
    public List<MediaInfo> Media { get; set; } = [];
}

public class MediaInfo
{
    public Guid? PreviewFileId { get; set; }
    public Guid? OriginalFileId { get; set; }
    public MediaType Type { get; set; }
}

public enum MediaType
{
    Photo,
    Video,
}

public record DeleteMessageRequest
{
    public Guid UserId { get; set; }
    public long MessageId { get; set; }
}

public record CreateMessageRequest
{
    public Guid UserId { get; set; }
    public long CategoryId { get; set; }
    public long StatusId { get; set; }
    public required string Content { get; set; }
}

public record EditMessageRequest
{
    public Guid UserId { get; set; }
    public long MessageId { get; set; }
    public required string Content { get; set; }
}

public record SearchRequest : IPaginationData
{
    public Guid UserId { get; set; }
    public long? CategoryId { get; set; }
    public string? SearchString { get; set; }
    public int Page { get; init; }
    public int PerPage { get; init; }
}

public class MessageDetailDto
{
    public required long Id { get; set; }
    public required DateTime Time { get; set; }
    public required string? Sender { get; set; }
    public string? SenderInitial { get; set; }
    public required string? Content { get; set; }
    public required string? CategoryName { get; set; }
    public required string? CategoryColor { get; set; }
    public required string? StatusName { get; set; }
    public required string? StatusColor { get; set; }
}

public class MessageDetailDtoData
{
    public required long Id { get; set; }
    public required DateTime Time { get; set; }
    public required long TelegramId { get; set; }
    public required string? TelegramUsername { get; set; }
    public required string? TelegramFirstName { get; set; }
    public required string? TelegramLastName { get; set; }
    public required string? Content { get; set; }
    public required string? CategoryName { get; set; }
    public required string? CategoryColor { get; set; }
    public required string? StatusName { get; set; }
    public required string? StatusColor { get; set; }
}

public record BatchRequest
{
    public int Skip { get; set; }
    public int Take { get; set; }
}

public class BatchResult<T>
{
    public long Offset { get; set; }
    public bool HasNext { get; set; }
    public required IReadOnlyCollection<T> Data { get; set; }
}

public class InitialBatchResult<T> : BatchResult<T>
{
    public long TotalCount { get; set; }
}

public class ColumnSummary
{
    public required long Id { get; set; }
    public required string Name { get; set; }
    public required string? Color { get; set; }
    public required int Count { get; set; }
    public required int SortOrder { get; set; }
}

public record CategorySummary
{
    public required long Id { get; set; }
    public required string Name { get; set; }
    public required string? Color { get; set; }
    public required ColumnSummary[] Columns { get; set; }
}