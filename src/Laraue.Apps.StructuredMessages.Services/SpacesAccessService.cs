using Laraue.Apps.StructuredMessages.DataAccess;
using Laraue.Apps.StructuredMessages.DataAccess.Enums;
using Laraue.Apps.StructuredMessages.DataAccess.Models;
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
        Guid userId,
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
            return await map(GetAllSpacesQuery(authData));
        
        var organizationPermission = await accessService
            .GetGlobalOrganizationAccess(authData, cancellationToken);
        
        // The whole organization is available to read with all spaces
        if (organizationPermission.HasFlag(AccessLevel.ReadItems))
            return await map(GetAllSpacesQuery(authData));

        var globalSpacePermission = await accessService
            .GetGlobalOrganizationAccess(authData, cancellationToken);
        
        // All spaces are available
        if (globalSpacePermission.HasFlag(AccessLevel.ReadItems))
            return await map(GetAllSpacesQuery(authData, globalSpacePermission));

        // Part of spaces are available
        return await map(context.SpaceOrganizationUsers
            .Where(o => o.OrganizationUser!.OrganizationId == authData.OrganizationId)
            .Where(o => o.OrganizationUser!.UserId == authData.UserId)
            .Select(o => new SpaceWithAccessLevel(o.Space!, o.AccessLevel)));
    }

    private IQueryable<SpaceWithAccessLevel> GetAllSpacesQuery(OrganizationAuthData authData)
    {
        return context.Spaces
            .Where(s => s.OrganizationId == authData.OrganizationId)
            .Select(s => new SpaceWithAccessLevel(s, s.IsDefault ? AccessLevel.UpdateItems : AccessLevel.Manage));  // TODO - Incorrect, level can be inherited. Why manage???
    }
    
    private IQueryable<SpaceWithAccessLevel> GetAllSpacesQuery(OrganizationAuthData authData, AccessLevel accessLevel)
    {
        return context.Spaces
            .Where(s => s.OrganizationId == authData.OrganizationId)
            .Select(s => new SpaceWithAccessLevel(s, accessLevel));
    }

    public Task HasAccessOrThrow(Guid userId, long spaceId, AccessLevel accessLevel, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}

public record SpaceWithAccessLevel(Space Space, AccessLevel AccessLevel);