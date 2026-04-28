using Laraue.Apps.StructuredMessages.DataAccess;
using Laraue.Apps.StructuredMessages.DataAccess.Enums;
using LinqToDB.EntityFrameworkCore;

namespace Laraue.Apps.StructuredMessages.Services;

public interface IAccessService
{
    Task<ItemAccessLevel> GetGlobalOrganizationAccess(OrganizationAuthData authData, CancellationToken cancellationToken);
    Task<ItemAccessLevel> GetGlobalSpacesAccess(OrganizationAuthData authData, CancellationToken cancellationToken);
    Task<ItemAccessLevel> GetGlobalEpicsAccess(OrganizationAuthData authData, CancellationToken cancellationToken);
}

public class AccessService(DatabaseContext context) : IAccessService
{
    public Task<ItemAccessLevel> GetGlobalOrganizationAccess(OrganizationAuthData authData, CancellationToken cancellationToken)
    {
        return context.OrganizationUsers
            .Where(o => o.OrganizationId == authData.OrganizationId)
            .Where(o => o.UserId == authData.UserId)
            .Select(o => o.ItemAccessLevel)
            .FirstOrDefaultAsyncEF(cancellationToken);
    }

    public Task<ItemAccessLevel> GetGlobalSpacesAccess(OrganizationAuthData authData, CancellationToken cancellationToken)
    {
        return context.SpaceOrganizationUsers
            .Where(o => o.OrganizationUser!.OrganizationId == authData.OrganizationId)
            .Where(o => o.OrganizationUser!.UserId == authData.UserId)
            .Where(o => o.SpaceId == null)
            .Select(o => o.ItemAccessLevel)
            .FirstOrDefaultAsyncEF(cancellationToken);
    }

    public Task<ItemAccessLevel> GetGlobalEpicsAccess(OrganizationAuthData authData, CancellationToken cancellationToken)
    {
        return context.EpicOrganizationUsers
            .Where(o => o.OrganizationUser!.OrganizationId == authData.OrganizationId)
            .Where(o => o.OrganizationUser!.UserId == authData.UserId)
            .Where(o => o.EpicId == null)
            .Select(o => o.ItemAccessLevel)
            .FirstOrDefaultAsyncEF(cancellationToken);
    }
}