using Laraue.Apps.StructuredMessages.DataAccess;
using Laraue.Apps.StructuredMessages.DataAccess.Enums;
using LinqToDB.EntityFrameworkCore;

namespace Laraue.Apps.StructuredMessages.Services;

public interface IAccessService
{
    Task<ItemsAccessLevel> GetGlobalOrganizationAccess(OrganizationAuthData authData, CancellationToken cancellationToken);
    Task<ItemsAccessLevel> GetGlobalSpacesAccess(OrganizationAuthData authData, CancellationToken cancellationToken);
    Task<ItemsAccessLevel> GetGlobalEpicsAccess(OrganizationAuthData authData, CancellationToken cancellationToken);
}

public class AccessService(DatabaseContext context) : IAccessService
{
    public Task<ItemsAccessLevel> GetGlobalOrganizationAccess(OrganizationAuthData authData, CancellationToken cancellationToken)
    {
        return context.OrganizationUsers
            .Where(o => o.OrganizationId == authData.OrganizationId)
            .Where(o => o.UserId == authData.UserId)
            .Select(o => o.ItemsAccessLevel)
            .FirstOrDefaultAsyncEF(cancellationToken);
    }

    public Task<ItemsAccessLevel> GetGlobalSpacesAccess(OrganizationAuthData authData, CancellationToken cancellationToken)
    {
        return context.SpaceOrganizationUsers
            .Where(o => o.OrganizationUser!.OrganizationId == authData.OrganizationId)
            .Where(o => o.OrganizationUser!.UserId == authData.UserId)
            .Where(o => o.SpaceId == null)
            .Select(o => o.ItemsAccessLevel)
            .FirstOrDefaultAsyncEF(cancellationToken);
    }

    public Task<ItemsAccessLevel> GetGlobalEpicsAccess(OrganizationAuthData authData, CancellationToken cancellationToken)
    {
        return context.EpicOrganizationUsers
            .Where(o => o.OrganizationUser!.OrganizationId == authData.OrganizationId)
            .Where(o => o.OrganizationUser!.UserId == authData.UserId)
            .Where(o => o.EpicId == null)
            .Select(o => o.ItemsAccessLevel)
            .FirstOrDefaultAsyncEF(cancellationToken);
    }
}