using System.Linq.Expressions;
using Laraue.Apps.StructuredMessages.DataAccess;
using Laraue.Apps.StructuredMessages.DataAccess.Enums;
using Laraue.Apps.StructuredMessages.DataAccess.Models;
using Laraue.Core.DataAccess.EFCore.Extensions;
using Laraue.Core.Exceptions.Web;
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
    /// TODO - do not throw inside a method. Only return true false and throw with custom exception always
    Task HasAccessOrThrow(
        OrganizationAuthData authData,
        long epicId,
        ChildrenAccessLevel childrenAccessLevel,
        CancellationToken cancellationToken);
    
    Task<ChildrenAccessLevel> GetChildrenAccessLevel(
        OrganizationAuthData authData,
        long epicId,
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
    public long? EpicId { get; set; }
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
        
        var accessToViewEpic = accessLevels.EpicsAccessLevel;
        if (accessToViewEpic.HasFlag(ChildrenAccessLevel.Read))
            return await map(GetGlobalReadableEpicsQuery(authData, accessToViewEpic.ToEntityAccessLevel(), filter));
        
        return await map(GetDirectReadableEpicsQuery(authData, filter));
    }

    public async Task HasAccessOrThrow(
        OrganizationAuthData authData,
        long epicId,
        ChildrenAccessLevel childrenAccessLevel,
        CancellationToken cancellationToken)
    {
        var result = await GetChildrenAccessLevel(authData, epicId, cancellationToken);
        if (!result.HasFlag(childrenAccessLevel))
            throw new NotFoundException(
                $"Epic is unavailable or children permission: {childrenAccessLevel} is missing");
    }

    public async Task<ChildrenAccessLevel> GetChildrenAccessLevel(
        OrganizationAuthData authData,
        long epicId,
        CancellationToken cancellationToken)
    {
        var globalLevels = await accessService
            .GetChildrenAccessLevels(authData, cancellationToken);

        var result = globalLevels.IssuesAccessLevel;

        var epic = await context.Epics
            .Where(e => e.Id == epicId)
            .Select(x => new { x.SpaceId })
            .FirstOrThrowNotFoundEFAsync("Epic is not found", cancellationToken);
        
        var spaceDirectPermissions = await context.DirectSpacePermissions
            .Where(sos => sos.OrganizationUser!.OrganizationId == authData.OrganizationId)
            .Where(sos => sos.OrganizationUser!.UserId == authData.UserId)
            .Where(sos => sos.SpaceId == epic.SpaceId)
            .Select(sos => new { sos.ChildrenIssuesAccessLevel })
            .FirstOrDefaultAsyncLinqToDB(cancellationToken);
        
        if (spaceDirectPermissions is not null)
            result |= spaceDirectPermissions.ChildrenIssuesAccessLevel;
        
        var epicDirectPermissions = await context.DirectEpicPermissions
            .Where(eou => eou.OrganizationUser!.OrganizationId == authData.OrganizationId)
            .Where(eou => eou.OrganizationUser!.UserId == authData.UserId)
            .Where(eou => eou.EpicId == epicId)
            .Select(sos => new { sos.ChildrenIssuesAccessLevel })
            .FirstOrDefaultAsyncLinqToDB(cancellationToken);
        
        if (epicDirectPermissions is not null)
            result |= epicDirectPermissions.ChildrenIssuesAccessLevel;

        return result;
    }

    public async Task HasAccessOrThrow(
        OrganizationAuthData authData,
        long epicId,
        EntityAccessLevel entityAccessLevel,
        CancellationToken cancellationToken)
    {
        var accessLevels = await accessService
            .GetChildrenAccessLevels(authData, cancellationToken);

        var castedAccessLevel = entityAccessLevel.ToEntityAccessLevel();
        if (accessLevels.IssuesAccessLevel.HasFlag(castedAccessLevel))
            return;

        await context.DirectEpicPermissions
            .Where(dep => dep.OrganizationUser!.OrganizationId == authData.OrganizationId)
            .Where(dep => dep.OrganizationUser!.UserId == authData.UserId)
            .Where(dep => dep.ChildrenIssuesAccessLevel.HasFlag(castedAccessLevel))
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
        if (filter.EpicId.HasValue)
            query = query.Where(e => e.Id == filter.EpicId.Value);
        
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
                EntityAccessLevel = MergeGlobalAndDirectLevels(
                    epicData.DirectEpicPermission,
                    AdjustEpicAccessLevel(epicData.Epic, accessLevel)),
            });
    }
    
    private IQueryable<EpicWithAccessLevel> GetDirectReadableEpicsQuery(OrganizationAuthData authData, Filter filter)
    {
        var epicsQuery = context.Epics.AsQueryable();
        
        if (filter.SpaceId.HasValue)
            epicsQuery = epicsQuery.Where(e => e.Space!.Id == filter.SpaceId.Value);
        if (filter.EpicId.HasValue)
            epicsQuery = epicsQuery.Where(e => e.Id == filter.EpicId.Value);
        
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

    [ExpressionMethod(nameof(AdjustEpicAccessLevelImpl))]
    private static EntityAccessLevel AdjustEpicAccessLevel(Epic epic, EntityAccessLevel globalAccessLevel)
        => throw new InvalidOperationException("LINQ translation only.");
    
    private static Expression<Func<Epic, EntityAccessLevel, EntityAccessLevel>> AdjustEpicAccessLevelImpl()
        => (epic, globalAccessLevel) => epic.IsDefault
            ? globalAccessLevel & (EntityAccessLevel.Read | EntityAccessLevel.Update)
            : globalAccessLevel;
    
    [ExpressionMethod(nameof(MergeGlobalAndDirectLevelsImpl))]
    private static EntityAccessLevel MergeGlobalAndDirectLevels(DirectEpicPermission? directEpicPermission, EntityAccessLevel globalAccessLevel)
        => throw new InvalidOperationException("LINQ translation only.");
    
    private static Expression<Func<DirectEpicPermission?, EntityAccessLevel, EntityAccessLevel>> MergeGlobalAndDirectLevelsImpl()
        => (permission, globalAccessLevel) => permission != null
            ? permission.EntityAccessLevel | globalAccessLevel
            : globalAccessLevel;
}

public record EpicWithAccessLevel
{
    public required Epic Epic { get; init; }
    public required EntityAccessLevel EntityAccessLevel { get; init; }
}