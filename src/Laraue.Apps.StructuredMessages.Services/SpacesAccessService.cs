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
        AccessLevel accessLevel,
        CancellationToken cancellationToken);
}

public class SpacesAccessService(DatabaseContext context, IAccessService accessService) : ISpacesAccessService
{
    public async Task<T> GetAvailable<T>(
        OrganizationAuthData authData,
        Func<IQueryable<SpaceWithAccessLevel>, Task<T>> map,
        CancellationToken cancellationToken)
    {
        if (authData.OrganizationType is OrganizationType.Personal)
            return await map(GetPersonalSpacesQuery(authData));
        
        var globalOrganizationAccess = await accessService
            .GetGlobalOrganizationAccess(authData, cancellationToken);

        var globalSpaceAccess = await accessService
            .GetGlobalOrganizationAccess(authData, cancellationToken);

        var topLevelAccess = globalOrganizationAccess & globalSpaceAccess;
        return await map(GetAllSpacesQuery(authData, topLevelAccess));
    }

    public async Task HasAccessOrThrow(
        OrganizationAuthData authData,
        long spaceId,
        AccessLevel accessLevel,
        CancellationToken cancellationToken)
    {
        if (authData.OrganizationType is OrganizationType.Personal)
            return;
        
        var globalOrganizationAccess = await accessService
            .GetGlobalOrganizationAccess(authData, cancellationToken);
        
        if (globalOrganizationAccess.HasFlag(accessLevel))
            return;
        
        var globalSpaceAccess = await accessService
            .GetGlobalOrganizationAccess(authData, cancellationToken);
        
        if (globalSpaceAccess.HasFlag(accessLevel))
            return;

        await context.SpaceOrganizationUsers
            .Where(sos => sos.OrganizationUser!.OrganizationId == authData.OrganizationId)
            .Where(sos => sos.OrganizationUser!.UserId == authData.UserId)
            .AnyOrThrowNotFoundEFAsync(
                sos => sos.AccessLevel.HasFlag(accessLevel),
                $"Space: {spaceId} is unavailable or permission: {accessLevel} is missing",
                cancellationToken);
    }

    private IQueryable<SpaceWithAccessLevel> GetPersonalSpacesQuery(OrganizationAuthData authData)
    {
        return context.Spaces
            .Where(s => s.OrganizationId == authData.OrganizationId)
            .Select(s => new SpaceWithAccessLevel(s, AccessLevel.All));
    }
    
    private IQueryable<SpaceWithAccessLevel> GetAllSpacesQuery(OrganizationAuthData authData, AccessLevel topLevelAccess)
    {
        return context.SpaceOrganizationUsers
            .Where(sos => sos.OrganizationUser!.OrganizationId == authData.OrganizationId)
            .Where(sos => sos.OrganizationUser!.UserId == authData.UserId)
            .Select(sos => new SpaceWithAccessLevel(sos.Space!, topLevelAccess & sos.AccessLevel));
    }
}

public record SpaceWithAccessLevel(Space Space, AccessLevel AccessLevel);