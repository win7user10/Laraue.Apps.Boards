using Laraue.Apps.Boards.DataAccess;
using Laraue.Apps.Boards.DataAccess.Models;
using Laraue.Core.DataAccess.EFCore.Extensions;
using Laraue.Core.DateTime.Services.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace Laraue.Apps.Boards.Services;

public interface ICoreIssuesService
{
    Task<long> Create(
        CreateIssueRequest request,
        CancellationToken cancellationToken);
    
    Task Update(
        long messageId,
        Action<UpdateSettersBuilder<Issue>> setters,
        CancellationToken cancellationToken);
    
    Task Delete(
        long id,
        CancellationToken cancellationToken);
}

public class CoreIssuesService(
    DatabaseContext context,
    IDateTimeProvider dateTimeProvider,
    ISpaceCounterService spaceCounterService)
    : ICoreIssuesService
{
    public async Task<long> Create(
        CreateIssueRequest request,
        CancellationToken cancellationToken)
    {
        context.Database.EnsureTransactionStarted();
        
        var spaceId = await context.Statuses
            .Where(x => x.Id == request.StatusId)
            .Select(x => x.Epic!.SpaceId)
            .FirstOrThrowNotFoundEFAsync("Space was not found", cancellationToken);
        
        var issue = new Issue
        {
            Content = request.Text,
            UserId = request.UserId,
            CreatedAt = request.CreatedAt,
            UpdatedAt = request.CreatedAt,
            TelegramMessageId = request.TelegramMessageId,
            StatusId = request.StatusId,
        };
        
        var issueNumber = new IssueNumber
        {
            Number = await spaceCounterService.GetNextNumber(spaceId, cancellationToken),
            Issue = issue,
            SpaceId = spaceId,
        };
        
        context.Add(issue);
        context.Add(issueNumber);
        
        await context.SaveChangesAsync(cancellationToken);
        await TouchMessageBoard(issue.Id, request.CreatedAt, cancellationToken);
        
        return issue.Id;
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

    public Task Delete(long id, CancellationToken cancellationToken)
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

public class CreateIssueRequest
{
    public Guid UserId { get; set; }
    public required string? Text { get; set; }
    public required DateTime CreatedAt { get; set; }
    public long? TelegramMessageId { get; set; }
    public long StatusId { get; set; }
}