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
    
    Task<long> SaveMessage(
        SaveMessageRequest request,
        CancellationToken cancellationToken);
    
    Task UpdateMessage(
        long messageId,
        Action<UpdateSettersBuilder<Issue>> setters,
        CancellationToken cancellationToken);
    
    Task UpdateStatus(
        long messageId,
        long newStatusId,
        CancellationToken ct);
    
    Task UpdateCategory(
        long messageId,
        long newCategoryId,
        CancellationToken ct);
    
    Task DeleteMessage(
        long id,
        CancellationToken cancellationToken);
}

public class CoreIssuesService(DatabaseContext context, IDateTimeProvider dateTimeProvider)
    : ICoreIssuesService
{
    public const long NullId = 0;
    
    public Task<bool> UserHasAccessToMessage(Guid userId, long id, CancellationToken cancellationToken)
    {
        return context.Issues
            .Where(x => x.UserId == userId)
            .Where(x => x.Id == id)
            .AnyAsyncEF(cancellationToken);
    }

    public async Task<long> SaveMessage(
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
        
        var entity = new Issue
        {
            Content = request.Text,
            UserId = request.UserId,
            CreatedAt = request.CreatedAt,
            UpdatedAt = request.CreatedAt,
            TelegramMessageId = request.TelegramMessageId,
            EpicId = request.CategoryId,
            StatusId = statusId,
        };
        
        context.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        
        await TouchMessageBoard(entity.Id, request.CreatedAt, cancellationToken);
        
        return entity.Id;
    }

    public async Task UpdateMessage(
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

    public async Task UpdateStatus(long messageId, long newStatusId, CancellationToken ct)
    {
        if (newStatusId != NullId)
        {
            var possibleStatusesIds = await context.Issues
                .Where(x => x.Id == messageId)
                .Select(x => x.Epic!.Statuses!
                    .Select(s => s.Id))
                .FirstOrThrowNotFoundEFAsync(ct);

            if (!possibleStatusesIds.Contains(newStatusId))
                throw new BadRequestException(
                    nameof(newStatusId),
                    "Incorrect Status Id");
        }

        long? value = newStatusId == NullId ? null : newStatusId;
        await UpdateMessage(
            messageId,
            update => update.SetProperty(x => x.StatusId, value),
            ct);
    }

    public async Task UpdateCategory(long messageId, long newCategoryId, CancellationToken ct)
    {
        if (newCategoryId != NullId)
        {
            var userId = await context.Issues
                .Where(x => x.Id == messageId)
                .Select(x => x.UserId)
                .FirstOrDefaultAsyncEF(ct);
        
            var possibleCategoryIds = await context.Epics
                .Where(x => x.UserId == userId)
                .Select(x => x.Id)
                .ToArrayAsyncEF(ct);
        
            if (!possibleCategoryIds.Contains(newCategoryId))
                throw new BadRequestException(
                    nameof(newCategoryId),
                    "Incorrect Category Id");
        }
        
        long? value = newCategoryId == NullId ? null : newCategoryId;
        
        // Set default category status after moving to category
        long? newStatus = null;
        if (value is not null)
        {
            var newStatusData = await context.Statuses
                .Where(s => s.EpicId == value.Value)
                .OrderBy(s => s.SortOrder)
                .Select(s => new { s.Id })
                .FirstOrDefaultAsyncEF(ct);
            
            newStatus = newStatusData?.Id;
        }
        
        await UpdateMessage(
            messageId,
            update => update
                .SetProperty(x => x.EpicId, value)
                .SetProperty(x => x.StatusId, newStatus),
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
}