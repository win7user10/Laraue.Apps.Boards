using Laraue.Apps.StructuredMessages.DataAccess;
using Laraue.Apps.StructuredMessages.DataAccess.Models;
using Laraue.Core.DataAccess.EFCore.Extensions;
using Laraue.Core.DateTime.Services.Abstractions;
using Laraue.Core.Exceptions.Web;
using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace Laraue.Apps.StructuredMessages.Services;

public interface ICoreIssuesService
{
    Task<bool> UserHasAccessToMessage(
        Guid userId,
        long id,
        CancellationToken cancellationToken);
    
    Task<long> Create(
        SaveMessageRequest request,
        CancellationToken cancellationToken);
    
    Task Update(
        long messageId,
        Action<UpdateSettersBuilder<Issue>> setters,
        CancellationToken cancellationToken);
    
    Task Move(
        long issueId,
        long spaceId,
        long epicId,
        long statusId,
        CancellationToken ct);
    
    Task DeleteMessage(
        long id,
        CancellationToken cancellationToken);
}

public class CoreIssuesService(DatabaseContext context, IDateTimeProvider dateTimeProvider)
    : ICoreIssuesService
{
    public Task<bool> UserHasAccessToMessage(Guid userId, long id, CancellationToken cancellationToken)
    {
        return context.Issues
            .Where(x => x.UserId == userId)
            .Where(x => x.Id == id)
            .AnyAsyncEF(cancellationToken);
    }

    public async Task<long> Create(
        SaveMessageRequest request,
        CancellationToken cancellationToken)
    {
        var statusId = request.StatusId;
        
        if (request.CategoryId is null)
        {
            // If status is set, but category is not set it's wrong
            if (statusId is not null)
            {
                throw new BadRequestException(
                    nameof(statusId),
                    "To set the status need to specify category");
            }
        }
        else
        {
            // Check that status is correct
            var statusesAvailable = await context.Statuses
                .Where(x => x.EpicId == request.CategoryId)
                .OrderBy(x => x.SortOrder)
                .Select(x => x.Id)
                .ToArrayAsyncEF(cancellationToken);
            
            // If status is passed, check that is correct
            if (statusId is not null)
            {
                if (!statusesAvailable.Contains(statusId.Value))
                    throw new BadRequestException(
                        nameof(request.StatusId),
                        "Status is not found in the category");
            }
            // If status is not passed, select the default available status
            else
            {
                statusId ??= statusesAvailable.FirstOrDefault();
            }
        }

        if (request.SpaceId is not null && request.CategoryId is not null)
        {
            // Check that epic is correct
            var epicsAvailable = await context.Epics
                .Where(x => x.SpaceId == request.SpaceId)
                .Select(x => x.Id)
                .ToArrayAsyncEF(cancellationToken);
            
            if (!epicsAvailable.Contains(request.CategoryId.Value))
                throw new BadRequestException(
                    nameof(request.StatusId),
                    "Epic is not found in the space");
        }
        
        var entity = new Issue
        {
            Content = request.Text,
            UserId = request.UserId,
            CreatedAt = request.CreatedAt,
            UpdatedAt = request.CreatedAt,
            TelegramMessageId = request.TelegramMessageId,
            EpicId = request.CategoryId,
            StatusId = statusId,
            SpaceId = request.SpaceId,
        };
        
        context.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        
        await TouchMessageBoard(entity.Id, request.CreatedAt, cancellationToken);
        
        return entity.Id;
    }

    public async Task Update(
        long messageId,
        Action<UpdateSettersBuilder<Issue>> setters,
        CancellationToken cancellationToken)
    {
        var date = dateTimeProvider.UtcNow;

        await context.Issues
            .Where(x => x.Id == messageId)
            .ExecuteUpdateAsync(
                upd =>
                {
                    setters(upd);
                    upd.SetProperty(x => x.UpdatedAt, date);
                },
                cancellationToken);
        
        await TouchMessageBoard(messageId, date, cancellationToken);
    }

    public async Task Move(
        long issueId,
        long spaceId,
        long epicId,
        long statusId,
        CancellationToken ct)
    {
        var currentState = await context.Issues
            .Where(x => x.Id == issueId)
            .Select(x => new
            {
                x.EpicId,
                x.StatusId,
                x.SpaceId,
            })
            .FirstOrThrowNotFoundEFAsync(ct);
        
        // Validate new space
        var newSpaceId = IdService.ToNullableId(spaceId);
        var updateSpace = newSpaceId != currentState.SpaceId;
        if (updateSpace && newSpaceId.HasValue)
        {
            var userId = await context.Issues
                .Where(x => x.Id == issueId)
                .Select(x => x.UserId)
                .FirstOrDefaultAsyncEF(ct);
        
            var possibleSpacesIds = await context.Spaces
                .Where(x => x.CreatorId == userId)
                .Select(x => x.Id)
                .ToArrayAsyncEF(ct);
        
            if (!possibleSpacesIds.Contains(newSpaceId.Value))
                throw new BadRequestException(
                    nameof(spaceId),
                    "Incorrect Space Id");
        }
        
        // Validate new epic
        var newEpicId = IdService.ToNullableId(epicId);
        var updateEpic = newEpicId != currentState.EpicId;
        if (updateEpic && newEpicId.HasValue)
        {
            var possibleEpicIds = await context.Epics
                .Where(x => x.SpaceId == newSpaceId)
                .Select(x => x.Id)
                .ToArrayAsyncEF(ct);
        
            if (!possibleEpicIds.Contains(newEpicId.Value))
                throw new BadRequestException(
                    nameof(epicId),
                    "Incorrect Epic Id");
        }
        
        // Validate new status
        var newStatusId = IdService.ToNullableId(statusId);
        var updateStatus = newStatusId != currentState.StatusId;
        if (updateStatus && newStatusId.HasValue)
        {
            var possibleStatusesIds = await context.Statuses
                .Where(x => x.EpicId == newEpicId)
                .Select(x => x.Id)
                .ToListAsyncEF(ct);

            if (!possibleStatusesIds.Contains(newStatusId.Value))
                throw new BadRequestException(
                    nameof(statusId),
                    "Incorrect Status Id");
        }

        await Update(
            issueId,
            update => update
                .SetProperty(x => x.StatusId, newStatusId)
                .SetProperty(x => x.SpaceId, newSpaceId)
                .SetProperty(x => x.EpicId, newEpicId),
            ct);
    }

    public Task DeleteMessage(long id, CancellationToken cancellationToken)
    {
        return context.Issues
            .Where(x => x.Id == id)
            .ExecuteDeleteAsync(cancellationToken);
    }

    private Task<int> TouchMessageBoard(long issueId, DateTime touchedAt, CancellationToken ct)
    {
        return context.Issues.Where(x => x.Id == issueId)
            .Select(x => x.Epic)
            .ExecuteUpdateAsync(x => x
                .SetProperty(
                    p => p!.TouchedAt,
                    old => old!.TouchedAt > touchedAt ? old.TouchedAt : touchedAt),
                ct);
    }
}

public class SaveMessageRequest
{
    public Guid UserId { get; set; }
    public required string? Text { get; set; }
    public required DateTime CreatedAt { get; set; }
    public long? TelegramMessageId { get; set; }
    public long? CategoryId { get; set; }
    public long? StatusId { get; set; }
    public long? SpaceId { get; set; }
}