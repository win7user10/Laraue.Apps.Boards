using Laraue.Apps.StructuredMessages.DataAccess;
using Laraue.Apps.StructuredMessages.DataAccess.Enums;
using Laraue.Apps.StructuredMessages.DataAccess.Models;
using Laraue.Core.Exceptions.Web;
using LinqToDB.EntityFrameworkCore;

namespace Laraue.Apps.StructuredMessages.Services;

public interface IIssuesAccessService
{
    Task<T> GetAvailable<T>(
        OrganizationAuthData authData,
        Func<IQueryable<Issue>, Task<T>> map,
        CancellationToken cancellationToken);
    
    Task<EntityAccessLevel> GetAccessLevel(
        OrganizationAuthData authData,
        long issueId,
        CancellationToken cancellationToken);
    
    Task HasAccessOrThrow(
        OrganizationAuthData authData,
        long issueId,
        EntityAccessLevel entityAccessLevel,
        CancellationToken cancellationToken);
}

public class IssuesAccessService(IEpicsAccessService epicsAccessService, DatabaseContext context) : IIssuesAccessService
{
    public Task<T> GetAvailable<T>(
        OrganizationAuthData authData,
        Func<IQueryable<Issue>, Task<T>> map,
        CancellationToken cancellationToken)
    {
        return epicsAccessService.GetAvailable(
            authData,
            epics => map(epics
                .SelectMany(e => e.Epic.Statuses!
                    .SelectMany(i => i.Issues!))),
            cancellationToken);
    }

    public async Task<EntityAccessLevel> GetAccessLevel(OrganizationAuthData authData, long issueId, CancellationToken cancellationToken)
    {
        var epicData = await context.Issues
            .Where(i => i.Id == issueId)
            .Select(x => new { x.Status!.EpicId })
            .FirstOrDefaultAsyncEF(cancellationToken);

        if (epicData == null)
            return EntityAccessLevel.None;

        var issuesAccessLevel = await epicsAccessService.GetChildrenAccessLevel(
            authData,
            epicData.EpicId,
            cancellationToken);

        return issuesAccessLevel.ToEntityAccessLevel();
    }

    public async Task HasAccessOrThrow(
        OrganizationAuthData authData,
        long issueId,
        EntityAccessLevel entityAccessLevel,
        CancellationToken cancellationToken)
    {
        var level = await GetAccessLevel(authData, issueId, cancellationToken);
        if (!level.HasFlag(entityAccessLevel))
            throw new NotFoundException($"Issue: {issueId} is not exists or epic children permission: {entityAccessLevel} is missing");
    }
}