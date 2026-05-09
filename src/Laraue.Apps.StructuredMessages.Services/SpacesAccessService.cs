using Laraue.Apps.StructuredMessages.DataAccess;
using Laraue.Apps.StructuredMessages.DataAccess.Enums;
using Laraue.Apps.StructuredMessages.DataAccess.Models;
using Laraue.Core.DataAccess.EFCore.Extensions;
using LinqToDB;

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
    
    Task CanCreateEpics(
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

    public async Task CanCreateEpics(
        OrganizationAuthData authData,
        long spaceId,
        CancellationToken cancellationToken)
    {
        var accessLevels = await accessService
            .GetChildrenAccessLevels(authData, cancellationToken);
        
        if (accessLevels.EpicsAccessLevel.HasFlag(ChildrenAccessLevel.Create))
            return;

        await context.DirectSpacePermissions
            .Where(sos => sos.OrganizationUser!.OrganizationId == authData.OrganizationId)
            .Where(sos => sos.OrganizationUser!.UserId == authData.UserId)
            .Where(sos => sos.ChildrenEpicsAccessLevel.HasFlag(ChildrenAccessLevel.Create))
            .FirstOrThrowNotFoundEFAsync(
                sos => sos.SpaceId == spaceId,
                $"Space: {spaceId} is not exists or items permission: {ChildrenAccessLevel.Create} is missing",
                cancellationToken);
    }

    private IQueryable<SpaceWithAccessLevel> GetGlobalReadableSpacesQuery(OrganizationAuthData authData, EntityAccessLevel accessLevel)
    {
        return context.Spaces
            .Where(s => s.OrganizationId == authData.OrganizationId)
            .LeftJoin(
                context.DirectSpacePermissions,
                (space, user) => space.Id == user.SpaceId,
                (space, user) => new { Space = space, DirectSpacePermission = (DirectSpacePermission?)user })
            .Select(spaceUser => new SpaceWithAccessLevel(
                spaceUser.Space,
                spaceUser.Space.IsDefault
                    ? EntityAccessLevel.Read | EntityAccessLevel.Update
                    : spaceUser.DirectSpacePermission != null
                        ? spaceUser.DirectSpacePermission.EntityAccessLevel | accessLevel
                        : accessLevel));
    }
    
    private IQueryable<DirectSpacePermission> GetDirectReadableSpacesQuery(OrganizationAuthData authData)
    {
        return context.DirectSpacePermissions
            .Where(sos => sos.OrganizationUser!.OrganizationId == authData.OrganizationId)
            .Where(sos => sos.OrganizationUser!.UserId == authData.UserId)
            .Where(sos => sos.EntityAccessLevel.HasFlag(ChildrenAccessLevel.Read));
    }
}

public record SpaceWithAccessLevel(Space Space, EntityAccessLevel EntityAccessLevel);