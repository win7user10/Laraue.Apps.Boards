using Laraue.Apps.StructuredMessages.DataAccess;
using Laraue.Apps.StructuredMessages.DataAccess.Enums;
using Laraue.Apps.StructuredMessages.DataAccess.Extensions;
using Laraue.Apps.StructuredMessages.DataAccess.Models;
using Laraue.Apps.StructuredMessages.Services;
using Laraue.Core.DataAccess.Contracts;
using Laraue.Core.DataAccess.EFCore.Extensions;
using Laraue.Core.DataAccess.Extensions;
using Laraue.Core.DataAccess.Linq2DB.Extensions;
using Laraue.Core.DateTime.Services.Abstractions;
using Laraue.Core.Exceptions.Web;
using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Laraue.Apps.StructuredMessages.WebApiServices;

public interface IIssuesService
{
    Task<BatchResult<IssueListDto>> GetIssues(
        GetIssuesRequest request,
        CancellationToken cancellationToken);
    
    Task<ColumnMessages[]> GetBoard(
        GetBoardRequest request,
        CancellationToken cancellationToken);
    
    Task<CategorySummary[]> GetBoardSummary(
        GetBoardSummaryRequest request,
        CancellationToken cancellationToken);

    Task Move(
        MoveIssueRequest request,
        CancellationToken ct);
    
    Task Delete(
        DeleteIssueRequest request,
        CancellationToken ct);
    
    Task<long> Create(
        CreateIssueRequest request,
        CancellationToken ct);
    
    Task UpdateIssue(
        UpdateIssueRequest request,
        CancellationToken ct);
    
    Task<ShortPaginatedResult<IssueListDto>> Search(
        SearchRequest request,
        CancellationToken ct);
    
    Task<IssueDetailDto> GetIssue(
        GetIssueRequest request,
        CancellationToken cancellationToken);
}

public class IssuesService(
    DatabaseContext context,
    ICoreIssuesService messageService,
    IDateTimeProvider dateTimeProvider,
    IIssuesAccessService issuesAccessService,
    IEpicsAccessService epicsAccessService,
    IStatusAccessService statusAccessService)
    : IIssuesService
{
    public async Task<BatchResult<IssueListDto>> GetIssues(
        GetIssuesRequest request,
        CancellationToken cancellationToken)
    {
        var result = await issuesAccessService.GetAvailable(
            request.AuthData,
            (issues) =>
            {
                var query = issues
                    .Where(i => i.StatusId == request.StatusId);

                if (!string.IsNullOrEmpty(request.SearchString))
                {
                    query = query
                        .Where(x => x.Content!
                            .ILike(request.SearchString.AsSearchable()));
                }

                var ordered = query.OrderByDescending(x => x.Id);
                var projected = ProjectToTemporaryDto(ordered);
                return ToBatchResult(projected, request, cancellationToken);
            },
            cancellationToken);
        

        var projected = result.Data
            .Select(Map)
            .ToArray();
        
        await Enrich(projected, cancellationToken);

        return new BatchResult<IssueListDto>
        {
            HasNext = result.HasNext,
            Data = projected,
            Offset = result.Offset,
        };
    }

    private static async Task<BatchResult<T>> ToBatchResult<T>(
        IQueryable<T> queryable,
        BatchRequest request,
        CancellationToken cancellationToken)
    {
        var requested = await queryable
            .Skip(request.Skip)
            .Take(request.Take + 1)
            .ToListAsyncLinqToDB(cancellationToken);
        
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
        var statusIds = await context.Statuses
            .Where(x => x.EpicId == request.EpicId)
            .Select(x => x.Id)
            .ToListAsyncEF(cancellationToken);

        var result = new List<ColumnMessages>();
        foreach (var statusId in statusIds)
        {
            var query = context
                .Issues
                .Where(x => x.UserId == request.AuthData!.UserId)
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

            var mappedStatusResult = new InitialBatchResult<IssueListDto>()
            {
                Data = statusResult.Data.Select(Map).ToArray(),
                HasNext = statusResult.HasNextPage,
                Offset = statusResult.Data.Count,
                TotalCount = statusResult.Total,
            };
            
            result.Add(new ColumnMessages
            {
                StatusId = statusId,
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
        var spaceId = IdService.ToNullableId(request.SpaceId);
        
        var categoryById = (await context.Epics
            .Where(x => x.UserId == request.UserId)
            .Where(x => x.SpaceId == spaceId)
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
            .Where(x => x.Status!.Epic!.SpaceId == spaceId)
            .Select(x => x)
            .GroupBy(x => x.StatusId)
            .Select(x => new
            {
                Id = IdService.ToNotNullableId(x.Key),
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

    public async Task Move(MoveIssueRequest request, CancellationToken ct)
    {
        // Check that can move Issue
        await issuesAccessService.HasAccessOrThrow(
            request.AuthData,
            request.IssueId,
            EntityAccessLevel.Update,
            ct);
        
        // Check that can move to specified status
        await statusAccessService.CanMoveToStatusOrThrow(
            request.AuthData,
            request.StatusId,
            ct);
        
        await messageService.Move(
            request.IssueId,
            request.StatusId,
            ct);
    }

    public async Task Delete(DeleteIssueRequest request, CancellationToken ct)
    {
        await issuesAccessService.HasAccessOrThrow(
            request.AuthData,
            request.IssueId,
            EntityAccessLevel.Delete,
            ct);

        await messageService.Delete(request.IssueId, ct);
    }

    public async Task<long> Create(CreateIssueRequest request, CancellationToken ct)
    {
        var validationData = await context.Statuses
            .Where(s => s.Id == request.StatusId)
            .Select(x => new { x.EpicId, x.Epic!.SpaceId })
            .FirstOrThrowNotFoundEFAsync($"Status: {request.StatusId} is not found", ct);

        try
        {
            await epicsAccessService.HasAccessOrThrow(
                request.AuthData,
                validationData.EpicId,
                ChildrenAccessLevel.Create,
                ct);
        }
        catch (NotFoundException)
        {
            throw new NotFoundException(
                $"Status: {request.StatusId} is not found, or {ChildrenAccessLevel.Create} permission is missing for Epic contains this status");
        }

        return await messageService.Create(
            new SaveMessageRequest
            {
                CreatedAt = dateTimeProvider.UtcNow,
                Text = request.Content,
                UserId = request.AuthData.UserId,
                StatusId = request.StatusId,
            },
            ct);
    }

    public async Task UpdateIssue(UpdateIssueRequest request, CancellationToken ct)
    {
        await issuesAccessService.HasAccessOrThrow(
            request.AuthData,
            request.Id,
            EntityAccessLevel.Update,
            ct);
        
        await messageService.Update(
            request.Id,
            upd => upd
                .SetProperty(x => x.Content, request.Content),
            ct);
    }

    public async Task<ShortPaginatedResult<IssueListDto>> Search(
        SearchRequest request,
        CancellationToken ct)
    {
        var result = await issuesAccessService.GetAvailable(
            request.AuthData,
            issues =>
            {
                if (request.EpicId.HasValue)
                    issues = issues.Where(x => x.Status!.EpicId == request.EpicId);
        
                if (request.SpaceId.HasValue)
                    issues = issues.Where(x => x.Status!.Epic!.SpaceId == request.SpaceId);
        
                if (!string.IsNullOrEmpty(request.SearchString))
                    issues = issues
                        .Where(x => x.Content!.ILike(request.SearchString.AsSearchable()));

                return ProjectToTemporaryDto(issues.OrderByDescending(i => i.Id))
                    .ShortPaginateLinq2DbAsync(request, ct);
            }, ct);
        
        var mapped = result.MapTo(Map);
        await Enrich(mapped.Data, ct);
        
        return mapped;
    }

    public async Task<IssueDetailDto> GetIssue(
        GetIssueRequest request,
        CancellationToken cancellationToken)
    {
        await issuesAccessService.HasAccessOrThrow(
            request.AuthData,
            request.IssueId,
            EntityAccessLevel.Read,
            cancellationToken);

        var result = await context.Issues
            .Where(x => x.Id == request.IssueId)
            .Select(x => new MessageDetailDtoData
            {
                Id = x.Id,
                Content = x.Content,
                Time = x.CreatedAt,
                CategoryName = x.Status!.Epic!.Name,
                StatusName = x.Status!.Name,
                TelegramFirstName = x.User!.TelegramFirstName,
                TelegramLastName = x.User!.TelegramLastName,
                TelegramId = x.User.TelegramId,
                TelegramUsername = x.User.TelegramUserName,
                CategoryColor = x.Status.Epic.Color,
                StatusColor = x.Status.Color,
            })
            .FirstAsyncEF(cancellationToken);

        var sender = UserInitialsUtility.GetInitials(
            result.TelegramUsername,
            result.TelegramFirstName,
            result.TelegramLastName);;

        return new IssueDetailDto
        {
            Id = result.Id,
            Content = result.Content,
            Sender = sender.Sender,
            SenderInitial = sender.Initial,
            Time = result.Time,
            EpicName = result.CategoryName,
            StatusName = result.StatusName,
            EpicColor = result.CategoryColor,
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
            CategoryId = x.Status!.EpicId,
            StatusId = x.StatusId,
            TelegramFirstName = x.User!.TelegramFirstName,
            TelegramLastName = x.User!.TelegramLastName,
            TelegramId = x.User.TelegramId,
            TelegramUsername = x.User.TelegramUserName,
        });
    }
    
    private static IssueListDto Map(MessageListDtoData source)
    {
        var senderData = UserInitialsUtility.GetInitials(
            source.TelegramUsername,
            source.TelegramFirstName,
            source.TelegramLastName);

        return new IssueListDto
        {
            Id = source.Id,
            StatusId = source.StatusId,
            Content = source.Content,
            EpicId = source.CategoryId,
            Sender = senderData.Sender,
            SenderInitial = senderData.Initial,
            Time = source.Time
        };
    }
}

public record MoveIssueRequest
{
    public OrganizationAuthData AuthData { get; set; } = new();
    public long IssueId { get; set; }
    public long StatusId { get; set; }
}

public record GetIssuesRequest : BatchRequest
{
    public OrganizationAuthData AuthData { get; set; } = new();
    public long StatusId { get; set; }
    public string? SearchString { get; set; }
}

public record GetIssueRequest
{
    public OrganizationAuthData AuthData { get; set; } = new();
    public long IssueId { get; set; }
}

public record GetBoardRequest
{
    public OrganizationAuthData? AuthData { get; set; }
    public long EpicId { get; set; }
    public int Take { get; init; }
    public string? SearchString { get; init; }
}

public record GetBoardSummaryRequest
{
    public Guid UserId { get; set; }
    public long SpaceId { get; set; }
}

public record ColumnMessages
{
    public required long StatusId { get; set; }
    public required InitialBatchResult<IssueListDto> Items { get; set; }
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

public class IssueListDto : ICanContainMedia
{
    public required long Id { get; set; }
    public required DateTime Time { get; set; }
    public required string? Sender { get; set; }
    public string? SenderInitial { get; set; }
    public required string? Content { get; set; }
    public required long EpicId { get; set; }
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

public record DeleteIssueRequest
{
    public OrganizationAuthData AuthData { get; set; } = new();
    public long IssueId { get; set; }
}

public record CreateIssueRequest
{
    public OrganizationAuthData AuthData { get; set; } = new();
    public long StatusId { get; set; }
    public required string Content { get; set; }
}

public record UpdateIssueRequest
{
    public OrganizationAuthData AuthData { get; set; } = new();
    public long Id { get; set; }
    public required string Content { get; set; }
}

public record SearchRequest : IPaginationData
{
    public OrganizationAuthData AuthData { get; set; } = new();
    public long? EpicId { get; set; }
    public long? SpaceId { get; set; }
    public string? SearchString { get; set; }
    public int Page { get; init; }
    public int PerPage { get; init; }
}

public class IssueDetailDto
{
    public required long Id { get; set; }
    public required DateTime Time { get; set; }
    public required string? Sender { get; set; }
    public string? SenderInitial { get; set; }
    public required string? Content { get; set; }
    public required string? EpicName { get; set; }
    public required string? EpicColor { get; set; }
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