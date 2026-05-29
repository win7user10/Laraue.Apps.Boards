using Laraue.Apps.StructuredMessages.DataAccess;
using Laraue.Core.DataAccess.EFCore.Extensions;
using Laraue.Core.Exceptions.Web;
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Laraue.Apps.StructuredMessages.Services;

public interface ICoreMovementService
{
    /// <summary>
    /// Move space to the new organization.
    /// </summary>
    Task MoveSpace(
        long spaceId,
        long newOrganizationId,
        CancellationToken cancellationToken);
    
    /// <summary>
    /// Move space epics to the new space of any organization.
    /// </summary>
    Task MoveSpaceEpics(
        long spaceId,
        long newSpaceId,
        CancellationToken cancellationToken);
    
    /// <summary>
    /// Move epic to the new space of any organization.
    /// </summary>
    Task MoveEpic(
        long epicId,
        long newSpaceId,
        CancellationToken cancellationToken);
    
    /// <summary>
    /// Move issue to new status.
    /// </summary>
    Task MoveIssue(
        long issueId,
        long statusId,
        CancellationToken ct);
}

public class CoreMovementService(
    DatabaseContext context,
    ICoreIssuesService issuesService,
    ISpaceCounterService spaceCounterService)
    : ICoreMovementService
{
    public async Task MoveSpace(long spaceId, long newOrganizationId, CancellationToken cancellationToken)
    {
        var sourceData = await context.Spaces
            .Where(x => x.Id == spaceId)
            .Select(x => new { x.IsDefault, x.Key })
            .FirstOrThrowNotFoundEFAsync($"Space: {spaceId} is not found", cancellationToken);
        
        if (sourceData.IsDefault)
            throw new ForbiddenException("Default space cannot be moved.");
        
        var suchSpaceKeyExists = await context.Spaces
            .Where(x => x.OrganizationId == newOrganizationId)
            .Where(x => x.Key == sourceData.Key)
            .AnyAsyncEF(cancellationToken);
        
        if (suchSpaceKeyExists)
            throw new BadRequestException(
                nameof(newOrganizationId),
                $"Space key {sourceData.Key} already exists in target organization.");

        await context.Spaces
            .Where(x => x.Id == spaceId)
            .ExecuteUpdateAsync(u => u
                .SetProperty(p => p.OrganizationId, newOrganizationId),
                cancellationToken);
    }

    public async Task MoveSpaceEpics(long spaceId, long newSpaceId, CancellationToken cancellationToken)
    {
        context.Database.EnsureTransactionStarted();
        
        var updatedCount = await context.Epics
            .Where(x => x.SpaceId == spaceId)
            .Where(x => x.IsDefault == false)
            .ExecuteUpdateAsync(u => u
                .SetProperty(epic => epic.SpaceId, newSpaceId),
                cancellationToken);
        
        if (updatedCount == 0)
            return;

        var issuesNumbersToUpdateQuery = context.IssueNumbers
            .Where(i => i.SpaceId == spaceId);

        var issuesToUpdateQueryCount = await issuesNumbersToUpdateQuery
            .CountAsyncEF(cancellationToken);
        
        var nextNumber = await spaceCounterService.GetNextNumber(
            spaceId, issuesToUpdateQueryCount, cancellationToken);

        var issueNumbers = issuesNumbersToUpdateQuery
            .Select(number => new 
            {
                number.IssueId,
                RowNum = Sql.Ext.RowNumber().Over().OrderBy(number.IssueId).ToValue()
            })
            .AsCte();

        await context.IssueNumbers
            .Join(issueNumbers, number => number.IssueId, n => n.IssueId, (number, n) => new { Number = number, n })
            .Set(x => x.Number.Number, x => nextNumber + x.n.RowNum - 1)
            .Set(x => x.Number.SpaceId, newSpaceId)
            .UpdateAsync(cancellationToken);
    }

    public async Task MoveEpic(long epicId, long newSpaceId, CancellationToken cancellationToken)
    {
        // TODO - renumber issues
        
        var sourceData = await context.Epics
            .Where(x => x.Id == epicId)
            .Select(x => new { x.IsDefault })
            .FirstOrThrowNotFoundEFAsync($"Epic: {epicId} is not found", cancellationToken);
        
        if (sourceData.IsDefault)
            throw new ForbiddenException("Default epic cannot be moved.");
        
        await context.Epics
            .Where(x => x.Id == epicId)
            .ExecuteUpdateAsync(u => u
                .SetProperty(epic => epic.SpaceId, newSpaceId),
                cancellationToken);
    }
    
    public async Task MoveIssue(
        long issueId,
        long statusId,
        CancellationToken ct)
    {
        // TODO - renumber issues
        
        await issuesService.Update(
            issueId,
            update => update.SetProperty(x => x.StatusId, statusId),
            ct);
    }
}