using Laraue.Apps.StructuredMessages.DataAccess;
using Laraue.Apps.StructuredMessages.DataAccess.Enums;
using Laraue.Apps.StructuredMessages.DataAccess.Models;
using Laraue.Core.DataAccess.EFCore.Extensions;
using LinqToDB.EntityFrameworkCore;

namespace Laraue.Apps.StructuredMessages.Services;

public interface ISpacesAccessService
{
    /// <summary>
    /// Returns spaces available for the user.
    /// </summary>
    /// <remarks>Use only with Linq2DB provider.</remarks>
    Task<T> GetAvailable<T>(
        OrganizationAuthData authData,
        Func<IQueryable<SpaceWithAccessLevel>, Task<T>> map,
        CancellationToken cancellationToken);
    
    Task HasAccessOrThrow(
        OrganizationAuthData authData,
        long spaceId,
        ItemsAccessLevel itemsAccessLevel,
        CancellationToken cancellationToken);
}

public class SpacesAccessService(DatabaseContext context, IAccessService accessService) : ISpacesAccessService
{
    public async Task<T> GetAvailable<T>(
        OrganizationAuthData authData,
        Func<IQueryable<SpaceWithAccessLevel>, Task<T>> map,
        CancellationToken cancellationToken)
    {
        var globalOrganizationAccess = await accessService
            .GetGlobalOrganizationAccess(authData, cancellationToken);
        
        if (globalOrganizationAccess == ItemsAccessLevel.All)
            return await map(GetFullAccessSpacesQuery(authData));

        var globalSpaceAccess = await accessService
            .GetGlobalOrganizationAccess(authData, cancellationToken);

        var topLevelAccess = globalOrganizationAccess & globalSpaceAccess;
        return await map(GetAllSpacesQuery(authData, topLevelAccess));
    }

    public async Task HasAccessOrThrow(
        OrganizationAuthData authData,
        long spaceId,
        ItemsAccessLevel itemsAccessLevel,
        CancellationToken cancellationToken)
    {
        var globalOrganizationAccess = await accessService
            .GetGlobalOrganizationAccess(authData, cancellationToken);
        
        if (globalOrganizationAccess.HasFlag(itemsAccessLevel))
            return;
        
        var globalSpaceAccess = await accessService
            .GetGlobalOrganizationAccess(authData, cancellationToken);
        
        if (globalSpaceAccess.HasFlag(itemsAccessLevel))
            return;

        await context.SpaceOrganizationUsers
            .Where(sos => sos.OrganizationUser!.OrganizationId == authData.OrganizationId)
            .Where(sos => sos.OrganizationUser!.UserId == authData.UserId)
            .AnyOrThrowNotFoundEFAsync(
                sos => sos.ItemsAccessLevel.HasFlag(itemsAccessLevel),
                $"Space: {spaceId} is unavailable or permission: {itemsAccessLevel} is missing",
                cancellationToken);
    }

    private IQueryable<SpaceWithAccessLevel> GetFullAccessSpacesQuery(OrganizationAuthData authData)
    {
        return context.Spaces
            .Where(s => s.OrganizationId == authData.OrganizationId)
            .Select(s => new SpaceWithAccessLevel(s, ItemsAccessLevel.All));
    }
    
    private IQueryable<SpaceWithAccessLevel> GetAllSpacesQuery(OrganizationAuthData authData, ItemsAccessLevel topLevelItemsAccess)
    {
        return context.SpaceOrganizationUsers
            .Where(sos => sos.OrganizationUser!.OrganizationId == authData.OrganizationId)
            .Where(sos => sos.OrganizationUser!.UserId == authData.UserId)
            .Select(sos => new SpaceWithAccessLevel(sos.Space!, topLevelItemsAccess & sos.ItemsAccessLevel));
    }
}

public record SpaceWithAccessLevel(Space Space, ItemsAccessLevel ItemsAccessLevel);