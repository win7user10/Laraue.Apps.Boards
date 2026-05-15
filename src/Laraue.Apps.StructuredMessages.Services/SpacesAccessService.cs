using System.Linq.Expressions;
using Laraue.Apps.StructuredMessages.DataAccess;
using Laraue.Apps.StructuredMessages.DataAccess.Enums;
using Laraue.Apps.StructuredMessages.DataAccess.Models;
using Laraue.Core.DataAccess.EFCore.Extensions;
using LinqToDB;
using LinqToDB.EntityFrameworkCore;

namespace Laraue.Apps.StructuredMessages.Services;

public interface ISpacesAccessService
{
    /// <summary>
    /// Returns spaces available for the user.
    /// </summary>
    /// <remarks>Use only with Linq2DB provider.</remarks>
    Task<T> GetAvailableForRead<T>(
        OrganizationAuthData authData,
        Func<IQueryable<SpaceWithAccessLevel>, Task<T>> map,
        CancellationToken cancellationToken);
    
    Task HasAccessOrThrow(
        OrganizationAuthData authData,
        long spaceId,
        EntityAccessLevel entityAccessLevel,
        CancellationToken cancellationToken);
    
    Task<bool> CanCreateEpics(
        OrganizationAuthData authData,
        long spaceId,
        CancellationToken cancellationToken);
    
    Task<ChildrenAccessLevel> GetChildrenAccessLevel(
        OrganizationAuthData authData,
        long spaceId,
        CancellationToken cancellationToken);
}

public class SpacesAccessService(DatabaseContext context, IAccessService accessService) : ISpacesAccessService
{
    public async Task<T> GetAvailableForRead<T>(
        OrganizationAuthData authData,
        Func<IQueryable<SpaceWithAccessLevel>, Task<T>> map,
        CancellationToken cancellationToken)
    {
        var accessLevels = await accessService
            .GetChildrenAccessLevels(authData, cancellationToken);
        
        var accessToViewSpaces = accessLevels.SpacesAccessLevel;
        if (accessToViewSpaces.HasFlag(ChildrenAccessLevel.Read))
            return await map(GetGlobalReadableSpacesQuery(authData, accessLevels.SpacesAccessLevel.ToEntityAccessLevel()));
        
        return await map(GetDirectReadableSpacesQuery(authData)
            .Select(x => new SpaceWithAccessLevel(
                x.Space!,
                x.EntityAccessLevel))); 
    }

    public async Task HasAccessOrThrow(
        OrganizationAuthData authData,
        long spaceId,
        EntityAccessLevel entityAccessLevel,
        CancellationToken cancellationToken)
    {
        var accessLevels = await accessService
            .GetChildrenAccessLevels(authData, cancellationToken);
        
        if (accessLevels.SpacesAccessLevel.HasFlag((ChildrenAccessLevel)entityAccessLevel))
            return;

        await context.DirectSpacePermissions
            .Where(sos => sos.OrganizationUser!.OrganizationId == authData.OrganizationId)
            .Where(sos => sos.OrganizationUser!.UserId == authData.UserId)
            .Where(sos => sos.SpaceId == spaceId)
            .AnyOrThrowNotFoundEFAsync(
                sos => sos.EntityAccessLevel.HasFlag(entityAccessLevel),
                $"Space: {spaceId} is unavailable or permission: {entityAccessLevel} is missing",
                cancellationToken);
    }

    public async Task<bool> CanCreateEpics(
        OrganizationAuthData authData,
        long spaceId,
        CancellationToken cancellationToken)
    {
        var accessLevels = await accessService
            .GetChildrenAccessLevels(authData, cancellationToken);
        
        if (accessLevels.EpicsAccessLevel.HasFlag(ChildrenAccessLevel.Create))
            return true;

        return await context.DirectSpacePermissions
            .Where(sos => sos.OrganizationUser!.OrganizationId == authData.OrganizationId)
            .Where(sos => sos.OrganizationUser!.UserId == authData.UserId)
            .Where(sos => sos.ChildrenEpicsAccessLevel.HasFlag(ChildrenAccessLevel.Create))
            .AnyAsyncEF(
                sos => sos.SpaceId == spaceId,
                cancellationToken);
    }

    public async Task<ChildrenAccessLevel> GetChildrenAccessLevel(
        OrganizationAuthData authData,
        long spaceId,
        CancellationToken cancellationToken)
    {
        var globalLevels = await accessService
            .GetChildrenAccessLevels(authData, cancellationToken);
        
        var result = globalLevels.EpicsAccessLevel;
        
        var spaceDirectPermissions = await context.DirectSpacePermissions
            .Where(sos => sos.OrganizationUser!.OrganizationId == authData.OrganizationId)
            .Where(sos => sos.OrganizationUser!.UserId == authData.UserId)
            .Where(sos => sos.SpaceId == spaceId)
            .Select(sos => new { sos.ChildrenEpicsAccessLevel })
            .FirstOrDefaultAsyncLinqToDB(cancellationToken);
        
        if (spaceDirectPermissions is not null)
            result |= spaceDirectPermissions.ChildrenEpicsAccessLevel;

        return result;
    }

    private IQueryable<SpaceWithAccessLevel> GetGlobalReadableSpacesQuery(OrganizationAuthData authData, EntityAccessLevel accessLevel)
    {
        return context.Spaces
            .Where(s => s.OrganizationId == authData.OrganizationId)
            .LeftJoin(
                context.DirectSpacePermissions,
                (space, user) => space.Id == user.SpaceId,
                (space, user) => new SpaceWithPermission(space,user))
            .Select(spaceUser => new SpaceWithAccessLevel(
                spaceUser.Space,
                MergeGlobalAndDirectLevels(
                    spaceUser,
                    AdjustSpaceAccessLevel(spaceUser, accessLevel))));
    }

    [ExpressionMethod(nameof(AdjustSpaceAccessLevelImpl))]
    private static EntityAccessLevel AdjustSpaceAccessLevel(SpaceWithPermission spaceUser, EntityAccessLevel globalAccessLevel)
        => throw new InvalidOperationException("LINQ translation only.");
    
    private static Expression<Func<SpaceWithPermission, EntityAccessLevel, EntityAccessLevel>> AdjustSpaceAccessLevelImpl()
        => (spaceUser, globalAccessLevel) => spaceUser.Space.IsDefault
            ? globalAccessLevel & (EntityAccessLevel.Read | EntityAccessLevel.Update)
            : globalAccessLevel;

    [ExpressionMethod(nameof(MergeGlobalAndDirectLevelsImpl))]
    private static EntityAccessLevel MergeGlobalAndDirectLevels(SpaceWithPermission spaceUser, EntityAccessLevel globalAccessLevel)
        => throw new InvalidOperationException("LINQ translation only.");
    
    private static Expression<Func<SpaceWithPermission, EntityAccessLevel, EntityAccessLevel>> MergeGlobalAndDirectLevelsImpl()
        => (spaceUser, globalAccessLevel) => spaceUser.DirectSpacePermission != null
            ? spaceUser.DirectSpacePermission.EntityAccessLevel | globalAccessLevel
            : globalAccessLevel;

    private record SpaceWithPermission(Space Space, DirectSpacePermission? DirectSpacePermission);
    
    private IQueryable<DirectSpacePermission> GetDirectReadableSpacesQuery(OrganizationAuthData authData)
    {
        return context.DirectSpacePermissions
            .Where(sos => sos.OrganizationUser!.OrganizationId == authData.OrganizationId)
            .Where(sos => sos.OrganizationUser!.UserId == authData.UserId)
            .Where(sos => sos.EntityAccessLevel.HasFlag(ChildrenAccessLevel.Read));
    }
}

public record SpaceWithAccessLevel(Space Space, EntityAccessLevel EntityAccessLevel);