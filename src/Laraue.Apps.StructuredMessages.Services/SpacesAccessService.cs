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

public class SpacesAccessService(DatabaseContext context) : ISpacesAccessService
{
    public async Task<T> GetAvailable<T>(
        OrganizationAuthData authData,
        Func<IQueryable<SpaceWithAccessLevel>, Task<T>> map,
        CancellationToken cancellationToken)
    {
        if (authData.OrganizationType is OrganizationType.Personal)
            return await map(GetAllSpacesQuery(authData));

        var organizationPermission = await context.OrganizationUsers
            .Where(o => o.OrganizationId == authData.OrganizationId)
            .Where(o => o.UserId == authData.UserId)
            .Select(o => o.AccessLevel)
            .FirstOrDefaultAsyncEF(cancellationToken);
        
        // The whole organization is available with all spaces
        if (organizationPermission >= AccessLevel.Read)
            return await map(GetAllSpacesQuery(authData));

        var globalSpacePermission = await context.SpaceOrganizationUsers
            .Where(o => o.OrganizationUser!.OrganizationId == authData.OrganizationId)
            .Where(o => o.OrganizationUser!.UserId == authData.UserId)
            .Where(o => o.SpaceId == null)
            .Select(o => new { o.AccessLevel })
            .FirstOrDefaultAsyncEF(cancellationToken);
        
        // All spaces are available
        if (globalSpacePermission is not null)
            return await map(GetAllSpacesQuery(authData, globalSpacePermission.AccessLevel));

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
            .Select(s => new SpaceWithAccessLevel(s, s.IsDefault ? AccessLevel.Update : AccessLevel.Manage));
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