using Laraue.Apps.StructuredMessages.DataAccess;
using Laraue.Apps.StructuredMessages.DataAccess.Enums;
using Laraue.Apps.StructuredMessages.DataAccess.Models;

namespace Laraue.Apps.StructuredMessages.Services;

public interface IEpicsAccessService
{
    /// <summary>
    /// Returns epics available for the user.
    /// </summary>
    /// <remarks>Use only with Linq2DB provider.</remarks>
    Task<T> GetAvailable<T>(
        OrganizationAuthData authData,
        Func<IQueryable<Epic>, Task<T>> map,
        CancellationToken cancellationToken);
    
    Task HasAccessOrThrow(
        Guid userId,
        long epicId,
        AccessLevel accessLevel,
        CancellationToken cancellationToken);
}

public class EpicsAccessService(DatabaseContext context, IAccessService accessService) : IEpicsAccessService
{
    public async Task<T> GetAvailable<T>(
        OrganizationAuthData authData,
        Func<IQueryable<Epic>, Task<T>> map, CancellationToken cancellationToken)
    {
        if (authData.OrganizationType is OrganizationType.Personal)
            return await map(GetAllEpicsQuery(authData, AccessLevel.All));
        
        var organizationPermission = await accessService
            .GetGlobalOrganizationAccess(authData, cancellationToken);
        
        // All organization is available to read at least
        if (organizationPermission.HasFlag(AccessLevel.ReadItems))
            return await map(GetAllEpicsQuery(authData, organizationPermission));

        var globalSpacePermission = await accessService
            .GetGlobalSpacesAccess(authData, cancellationToken);
        
        // All spaces are available to read at least
        if (globalSpacePermission.HasFlag(AccessLevel.ReadItems))
            return await map(GetAllEpicsQuery(authData, globalSpacePermission));

        var globalEpicPermission = await accessService
            .GetGlobalEpicsAccess(authData, cancellationToken);
        
        // All epics are available to read at least
        if (globalSpacePermission.HasFlag(AccessLevel.ReadItems))
            return await map(GetAllEpicsQuery(authData, globalEpicPermission));
        
        // Global permissions are not set, try to compute available epics via direct permissions
        // Then make query with joining to this epic.
        var epicsAvailableFromSpacesDirectAccess = context.SpaceOrganizationUsers
            .Where(o => o.OrganizationUser!.OrganizationId == authData.OrganizationId)
            .Where(o => o.OrganizationUser!.UserId == authData.UserId)
            .SelectMany(o => o.Space!.Epics!.Select(e => e.Id));
        
        var epicsAvailableFromEpicsDirectAccess = context.EpicOrganizationUsers
            .Where(o => o.OrganizationUser!.OrganizationId == authData.OrganizationId)
            .Where(o => o.OrganizationUser!.UserId == authData.UserId)
            .Select(o => o.Id);

        var allAvailableEpics = epicsAvailableFromSpacesDirectAccess
            .Union(epicsAvailableFromEpicsDirectAccess)
            .Join(context.Epics, l => l, epic => epic.Id, (l, epic) => epic);
        
        return await map(allAvailableEpics);
    }

    public Task HasAccessOrThrow(Guid userId, long epicId, AccessLevel accessLevel, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
    
    
    private IQueryable<Epic> GetAllEpicsQuery(OrganizationAuthData authData, AccessLevel minAccessLevel)
    {
        return context.Epics
            .Where(e => e.Space!.OrganizationId == authData.OrganizationId);
    }
}