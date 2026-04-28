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
        ItemAccessLevel itemAccessLevel,
        CancellationToken cancellationToken);
}

public class SpacesAccessService(DatabaseContext context, IAccessService accessService) : ISpacesAccessService
{
    public async Task<T> GetAvailableForRead<T>(
        OrganizationAuthData authData,
        Func<IQueryable<SpaceWithAccessLevel>, Task<T>> map,
        CancellationToken cancellationToken)
    {
        var globalOrganizationAccess = await accessService
            .GetGlobalOrganizationAccess(authData, cancellationToken);

        var globalSpaceAccess = await accessService
            .GetGlobalSpacesAccess(authData, cancellationToken);

        var globalAccess = globalOrganizationAccess | globalSpaceAccess;
        if (globalAccess.HasFlag(ItemAccessLevel.Read))
            return await map(GetGlobalReadableSpacesQuery(authData, globalAccess));
        
        return await map(GetDirectReadableSpacesQuery(authData)); 
    }

    public async Task HasAccessOrThrow(
        OrganizationAuthData authData,
        long spaceId,
        ItemAccessLevel itemAccessLevel,
        CancellationToken cancellationToken)
    {
        var globalOrganizationAccess = await accessService
            .GetGlobalOrganizationAccess(authData, cancellationToken);
        
        if (globalOrganizationAccess.HasFlag(itemAccessLevel))
            return;
        
        var globalSpaceAccess = await accessService
            .GetGlobalSpacesAccess(authData, cancellationToken);
        
        if (globalSpaceAccess.HasFlag(itemAccessLevel))
            return;

        await context.SpaceOrganizationUsers
            .Where(sos => sos.OrganizationUser!.OrganizationId == authData.OrganizationId)
            .Where(sos => sos.OrganizationUser!.UserId == authData.UserId)
            .AnyOrThrowNotFoundEFAsync(
                sos => sos.ItemAccessLevel.HasFlag(itemAccessLevel),
                $"Space: {spaceId} is unavailable or permission: {itemAccessLevel} is missing",
                cancellationToken);
    }

    private IQueryable<SpaceWithAccessLevel> GetGlobalReadableSpacesQuery(OrganizationAuthData authData, ItemAccessLevel accessLevel)
    {
        return context.Spaces
            .Where(s => s.OrganizationId == authData.OrganizationId)
            .LeftJoin(
                context.SpaceOrganizationUsers,
                (space, user) => space.Id == user.SpaceId,
                (space, user) => new { Space = space, User = (SpaceOrganizationUser?)user })
            .Select(spaceUser => new SpaceWithAccessLevel(
                spaceUser.Space,
                spaceUser.User != null ? spaceUser.User.ItemAccessLevel | accessLevel : accessLevel));
    }
    
    private IQueryable<SpaceWithAccessLevel> GetDirectReadableSpacesQuery(OrganizationAuthData authData)
    {
        return context.SpaceOrganizationUsers
            .Where(sos => sos.OrganizationUser!.OrganizationId == authData.OrganizationId)
            .Where(sos => sos.OrganizationUser!.UserId == authData.UserId)
            .Where(sos => sos.ItemAccessLevel.HasFlag(ItemAccessLevel.Read))
            .Select(sos => new SpaceWithAccessLevel(sos.Space!, sos.ItemAccessLevel));
    }
}

public record SpaceWithAccessLevel(Space Space, ItemAccessLevel ItemAccessLevel);