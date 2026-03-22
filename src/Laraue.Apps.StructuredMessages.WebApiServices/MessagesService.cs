using Laraue.Apps.StructuredMessages.DataAccess;
using Laraue.Apps.StructuredMessages.DataAccess.Models;
using Laraue.Apps.StructuredMessages.Services;
using Laraue.Core.DataAccess.Contracts;
using Laraue.Core.DataAccess.EFCore.Extensions;
using Laraue.Core.DataAccess.Extensions;
using Laraue.Core.DateTime.Services.Abstractions;
using Laraue.Core.Exceptions.Web;
using LinqToDB.EntityFrameworkCore;

namespace Laraue.Apps.StructuredMessages.WebApiServices;

public interface IMessagesService
{
    Task<BatchResult<MessageListDto>> GetMessages(
        GetMessagesRequest request,
        CancellationToken cancellationToken);
    
    Task<ColumnMessages[]> GetBoard(
        GetBoardRequest request,
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
            .Messages
            .Where(x => x.UserId == request.UserId)
            .Where(x => x.StatusId == statusId)
            .OrderByDescending(x => x.Id);

        var result = await ToBatchResult(ProjectToTemporaryDto(query), request);
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
            statusIds = await context.MessageStatuses
                .Where(x => x.MessageCategoryId == categoryId.Value)
                .Where(x => x.MessageCategory!.UserId == request.UserId)
                .Select(x => (long?)x.Id)
                .ToListAsyncEF(cancellationToken);
        }

        if (statusIds.Count == 0)
            throw new NotFoundException();

        var result = new List<ColumnMessages>();
        foreach (var statusId in statusIds)
        {
            var statusResult = await ProjectToTemporaryDto(context
                .Messages
                .Where(x => x.UserId == request.UserId)
                .Where(x => x.StatusId == statusId)
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
        var query = context.Messages
            .Where(x => x.UserId == request.UserId);
        
        if (request.CategoryId.HasValue)
        {
            var categoryId = request.CategoryId == CoreMessageService.NullId
                ? null
                : request.CategoryId;

            query = query.Where(x => x.CategoryId == categoryId);
        }
        
        if (!string.IsNullOrEmpty(request.SearchString))
        {
            query = query
                .Where(x => x.Content!.Contains(request.SearchString));
        }

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

        var result = await context.Messages
            .Where(x => x.Id == request.MessageId)
            .Select(x => new MessageDetailDtoData
            {
                Id = x.Id,
                Content = x.Content,
                Time = x.CreatedAt,
                CategoryName = x.Category!.Name,
                StatusName = x.Status!.Name,
                TelegramFirstName = x.User!.TelegramFirstName,
                TelegramLastName = x.User!.TelegramLastName,
                TelegramId = x.User.TelegramId,
                TelegramUsername = x.User.TelegramUserName,
            })
            .FirstAsyncEF(cancellationToken);

        var sender = GetUserSender(result);

        return new MessageDetailDto
        {
            Id = result.Id,
            Content = result.Content,
            Sender = sender.Sender,
            SenderInitial = sender.Initial,
            Time = result.Time,
            CategoryName = result.CategoryName,
            StatusName = result.StatusName,
        };
    }

    private async Task Enrich<T>(IList<T> elements, CancellationToken ct)
        where T : class, ICanContainMedia
    {
        var ids = elements.Select(x => x.Id).ToList();

        var photosByMessageId = (await context.TelegramPhotos
            .Where(x => ids.Contains(x.MessageId))
            .Select(x => new
            {
                x.MessageId,
                x.TelegramFileId,
                x.PhotoType,
                x.GroupId,
            })
            .ToArrayAsyncEF(ct))
            .GroupBy(x => x.MessageId)
            .ToDictionary(
                x => x.Key,
                x => x
                    .GroupBy(y => y.GroupId)
                    .ToDictionary(
                        y => y.Key,
                        y => y
                            .Select(z => new { z.PhotoType, z.TelegramFileId })));
        
        var videosByMessageId = (await context.TelegramVideos
            .Where(x => ids.Contains(x.MessageId))
            .Select(x => new { x.MessageId, x.ThumbnailFileId, x.FileId })
            .ToArrayAsyncEF(ct))
            .GroupBy(x => x.MessageId)
            .ToDictionary(
                x => x.Key);
        
        foreach (var element in elements)
        {
            if (photosByMessageId.TryGetValue(element.Id, out var photos))
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
            
            if (videosByMessageId.TryGetValue(element.Id, out var videos))
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
        IQueryable<Message> queryable)
    {
        return queryable.Select(x => new MessageListDtoData
        {
            Id = x.Id,
            Content = x.Content,
            Time = x.CreatedAt,
            CategoryId = x.CategoryId ?? CoreMessageService.NullId,
            StatusId = x.StatusId ?? CoreMessageService.NullId,
            TelegramFirstName = x.User!.TelegramFirstName,
            TelegramLastName = x.User!.TelegramLastName,
            TelegramId = x.User.TelegramId,
            TelegramUsername = x.User.TelegramUserName,
        });
    }

    public interface IHasUserSender
    {
        public string? TelegramUsername { get; set; }
        public string? TelegramFirstName { get; set; }
        public string? TelegramLastName { get; set; }
        public long TelegramId { get; set; }
    }

    public class UserSender
    {
        public required string Sender { get; set; }
        public required string Initial { get; set; }
    }

    private static UserSender GetUserSender(IHasUserSender source)
    {
        var sender = source.TelegramUsername;
        var initial = sender?.Length > 1 ? sender[..2] : "";

        if (sender is null)
        {
            if (source.TelegramFirstName?.Length > 0 && source.TelegramLastName?.Length > 0)
            {
                sender = $"{source.TelegramFirstName} {source.TelegramLastName}";
                initial = $"{source.TelegramFirstName[0]}{source.TelegramLastName[0]}";
            }
            else if (source.TelegramFirstName?.Length > 1)
            {
                sender = source.TelegramFirstName;
                initial = source.TelegramFirstName[..1];
            }
            else if (source.TelegramLastName?.Length > 1)
            {
                sender = source.TelegramLastName;
                initial = source.TelegramLastName[..1];
            }
            else
            {
                sender = source.TelegramId.ToString();
                initial = "ID";
            }
        }

        return new UserSender
        {
            Sender = sender,
            Initial = initial
        };
    }
    
    private static MessageListDto Map(MessageListDtoData source)
    {
        var senderData = GetUserSender(source);

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
}

public record ColumnMessages
{
    public required long StatusId { get; set; }
    public required InitialBatchResult<MessageListDto> Items { get; set; }
}

public class MessageListDtoData : MessagesService.IHasUserSender
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
    public required string? StatusName { get; set; }
}

public class MessageDetailDtoData : MessagesService.IHasUserSender
{
    public required long Id { get; set; }
    public required DateTime Time { get; set; }
    public required long TelegramId { get; set; }
    public required string? TelegramUsername { get; set; }
    public required string? TelegramFirstName { get; set; }
    public required string? TelegramLastName { get; set; }
    public required string? Content { get; set; }
    public required string? CategoryName { get; set; }
    public required string? StatusName { get; set; }
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