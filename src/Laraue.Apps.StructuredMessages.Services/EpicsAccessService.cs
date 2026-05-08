using Laraue.Apps.StructuredMessages.DataAccess;
using Laraue.Apps.StructuredMessages.DataAccess.Enums;
using Laraue.Apps.StructuredMessages.DataAccess.Models;
using Laraue.Core.DataAccess.EFCore.Extensions;
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Laraue.Apps.StructuredMessages.Services;

/// <remarks>Use only with Linq2DB provider.</remarks>
public interface IEpicsAccessService
{
    /// <summary>
    /// Returns epics available to read for the user.
    /// </summary>
    Task<T> GetAvailable<T>(
        OrganizationAuthData authData,
        Func<IQueryable<EpicWithAccessLevel>, Task<T>> map,
        CancellationToken cancellationToken);
    
    /// <summary>
    /// Returns epics available to read for the user.
    /// </summary>
    Task<T> GetAvailable<T>(
        OrganizationAuthData authData,
        Filter filter,
        Func<IQueryable<EpicWithAccessLevel>, Task<T>> map,
        CancellationToken cancellationToken);
    
    /// <summary>
    /// Returns is entity permitted for a change operation. To check access for read use one of <c>GetAvailable</c> methods.
    /// </summary>
    Task HasAccessOrThrow(
        OrganizationAuthData authData,
        long epicId,
        ChildrenAccessLevel childrenAccessLevel,
        CancellationToken cancellationToken);
    
    Task HasAccessOrThrow(
        OrganizationAuthData authData,
        long epicId,
        EntityAccessLevel entityAccessLevel,
        CancellationToken cancellationToken);
}

public class Filter
{
    public long? SpaceId { get; set; }
}

public class EpicsAccessService(DatabaseContext context, IAccessService accessService) : IEpicsAccessService
{
    public Task<T> GetAvailable<T>(
        OrganizationAuthData authData,
        Func<IQueryable<EpicWithAccessLevel>, Task<T>> map, CancellationToken cancellationToken)
    {
        return GetAvailable(authData, new Filter(), map, cancellationToken);
    }

    public async Task<T> GetAvailable<T>(
        OrganizationAuthData authData,
        Filter filter,
        Func<IQueryable<EpicWithAccessLevel>, Task<T>> map,
        CancellationToken cancellationToken)
    {
        var accessLevels = await accessService
            .GetChildrenAccessLevels(authData, cancellationToken);
        
        var accessToViewEpic = accessLevels.EpicsAccessLevel | accessLevels.IssuesAccessLevel;
        if (accessToViewEpic.HasFlag(ChildrenAccessLevel.Read))
        {
            var accessToEditEpic = accessLevels.SpacesAccessLevel | accessLevels.EpicsAccessLevel;
            return await map(GetGlobalReadableEpicsQuery(authData, accessToEditEpic.ToEntityAccessLevel(), filter));
        }
        
        return await map(GetDirectReadableEpicsQuery(authData, filter));
    }

    public async Task HasAccessOrThrow(
        OrganizationAuthData authData,
        long epicId,
        ChildrenAccessLevel childrenAccessLevel,
        CancellationToken cancellationToken)
    {
        var accessLevels = await accessService
            .GetChildrenAccessLevels(authData, cancellationToken);
        
        if (accessLevels.IssuesAccessLevel.HasFlag(childrenAccessLevel))
            return;

        var epic = await context.Epics
            .Where(e => e.Id == epicId)
            .Select(x => new { x.SpaceId })
            .FirstOrThrowNotFoundEFAsync("Epic is not found", cancellationToken);
        
        var hasDirectAccessFromSpace = await context.DirectSpacePermissions
            .Where(sos => sos.OrganizationUser!.OrganizationId == authData.OrganizationId)
            .Where(sos => sos.OrganizationUser!.UserId == authData.UserId)
            .Where(sos => sos.SpaceId == epic.SpaceId)
            .AnyAsync(sos => sos.ChildrenIssuesAccessLevel.HasFlag(childrenAccessLevel), cancellationToken);
        
        if (hasDirectAccessFromSpace)
            return;
        
        await context.DirectEpicPermissions
            .Where(eou => eou.OrganizationUser!.OrganizationId == authData.OrganizationId)
            .Where(eou => eou.OrganizationUser!.UserId == authData.UserId)
            .Where(eou => eou.EpicId == epicId)
            .AnyOrThrowNotFoundEFAsync(
                eou => eou.ChildrenIssuesAccessLevel.HasFlag(childrenAccessLevel),
                $"Epic is unavailable or children permission: {childrenAccessLevel} is missing",
                cancellationToken);
    }

    public async Task HasAccessOrThrow(
        OrganizationAuthData authData,
        long epicId,
        EntityAccessLevel entityAccessLevel,
        CancellationToken cancellationToken)
    {
        var accessLevels = await accessService
            .GetChildrenAccessLevels(authData, cancellationToken);
        
        if (accessLevels.IssuesAccessLevel.HasFlag(entityAccessLevel))
            return;

        await context.DirectEpicPermissions
            .Where(dep => dep.OrganizationUser!.OrganizationId == authData.OrganizationId)
            .Where(dep => dep.OrganizationUser!.UserId == authData.UserId)
            .Where(dep => dep.ChildrenIssuesAccessLevel.HasFlag(entityAccessLevel))
            .AnyAsyncEF(sos => sos.EpicId == epicId, cancellationToken);
    }

    private IQueryable<EpicWithAccessLevel> GetGlobalReadableEpicsQuery(
        OrganizationAuthData authData,
        EntityAccessLevel accessLevel,
        Filter filter)
    {
        var query = context.Epics.AsQueryable();
        if (filter.SpaceId.HasValue)
            query = query.Where(e => e.SpaceId == filter.SpaceId.Value);
        
        return query
            .Where(s => s.Space!.OrganizationId == authData.OrganizationId)
            .LeftJoin(
                context.DirectEpicPermissions,
                (epic, user) => epic.Id == user.EpicId,
                (epic, user) => new { Epic = epic, DirectEpicPermission = (DirectEpicPermission?)user })
            .LeftJoin(
                context.DirectSpacePermissions,
                (join, user) => join.Epic.SpaceId == user.SpaceId,
                (join, user) => new
                {
                    join.Epic,
                    join.DirectEpicPermission,
                    DirectSpacePermission = (DirectSpacePermission?)user
                })
            .Select(epicData => new EpicWithAccessLevel
            {
                Epic = epicData.Epic,
                EntityAccessLevel = epicData.DirectEpicPermission != null
                    ? epicData.DirectEpicPermission.EntityAccessLevel | accessLevel
                    : accessLevel,
            });
    }
    
    private IQueryable<EpicWithAccessLevel> GetDirectReadableEpicsQuery(OrganizationAuthData authData, Filter filter)
    {
        var epicsQuery = context.Epics.AsQueryable();
        
        if (filter.SpaceId.HasValue)
            epicsQuery = epicsQuery.Where(e => e.Space!.Id == filter.SpaceId.Value);
        
        var all = epicsQuery
            .InnerJoin(
                context.Spaces,
                (e, s) => e.SpaceId == s.Id,
                (e, s) => new { Epic = e, Space = s })
            .LeftJoin(
                context.DirectSpacePermissions,
                (e, s) =>
                    e.Space.Id == s.SpaceId
                    && (
                        (s.ChildrenEpicsAccessLevel & ChildrenAccessLevel.Read) == ChildrenAccessLevel.Read 
                        || (s.EntityAccessLevel & EntityAccessLevel.Read) == EntityAccessLevel.Read)
                    && s.OrganizationUser!.UserId == authData.UserId
                    && s.OrganizationUser!.OrganizationId == authData.OrganizationId,
                (e, s) => new { e.Epic, e.Space, DirectSpacePermission = s })
            .LeftJoin(
                context.DirectEpicPermissions,
                (e, o) =>
                    e.Epic.Id == o.Id
                    && (
                        (o.ChildrenIssuesAccessLevel & ChildrenAccessLevel.Read) == ChildrenAccessLevel.Read 
                        || (o.EntityAccessLevel & EntityAccessLevel.Read) == EntityAccessLevel.Read)
                    && o.OrganizationUser!.UserId == authData.UserId
                    && o.OrganizationUser!.OrganizationId == authData.OrganizationId,
                (e, o) => new
                {
                    e.Epic,
                    e.Space,
                    DirectSpacePermission = (DirectSpacePermission?)e.DirectSpacePermission,
                    DirectEpicPermission = (DirectEpicPermission?)o
                })
            .Where(x => x.DirectEpicPermission != null || x.DirectSpacePermission != null)
            .Select(e => new EpicWithAccessLevel
            {
                Epic = e.Epic,
                EntityAccessLevel = e.DirectEpicPermission!.EntityAccessLevel
            });
        
        return all;
    }
}

public record EpicWithAccessLevel
{
    public required Epic Epic { get; init; }
    public required EntityAccessLevel EntityAccessLevel { get; init; }
}