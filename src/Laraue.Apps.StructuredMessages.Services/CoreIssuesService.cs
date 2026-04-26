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
        var entity = new Issue
        {
            Content = request.Text,
            UserId = request.UserId,
            CreatedAt = request.CreatedAt,
            UpdatedAt = request.CreatedAt,
            TelegramMessageId = request.TelegramMessageId,
            StatusId = request.StatusId,
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
        long statusId,
        CancellationToken ct)
    {
        await Update(
            issueId,
            update => update.SetProperty(x => x.StatusId, statusId),
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
            .Select(x => x.Status!.Epic)
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
    public long StatusId { get; set; }
}