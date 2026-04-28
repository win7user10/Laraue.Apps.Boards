using Laraue.Apps.StructuredMessages.DataAccess;
using Laraue.Apps.StructuredMessages.DataAccess.Enums;
using Laraue.Apps.StructuredMessages.DataAccess.Models;
using Laraue.Core.Exceptions.Web;
using LinqToDB.EntityFrameworkCore;

namespace Laraue.Apps.StructuredMessages.Services;

public interface IOrganizationAccessService
{
    Task<T> GetAvailable<T>(
        Guid userId,
        Func<IQueryable<OrganizationUser>, Task<T>> map);
    
    Task HasAccessOrThrow(
        OrganizationAuthData authData,
        ItemAccessLevel itemAccessLevel,
        CancellationToken cancellationToken);
    
    Task HasAccessOrThrow(
        OrganizationAuthData authData,
        AdminAccessLevel accessLevel,
        CancellationToken cancellationToken);
}

public class OrganizationAccessService(DatabaseContext context) : IOrganizationAccessService
{
    public Task<T> GetAvailable<T>(Guid userId, Func<IQueryable<OrganizationUser>, Task<T>> map)
    {
        var query = context.OrganizationUsers
            .Where(x => x.UserId == userId);
        
        return map(query);
    }

    public async Task HasAccessOrThrow(
        OrganizationAuthData authData,
        ItemAccessLevel itemAccessLevel,
        CancellationToken cancellationToken)
    {
        var hasAccess = await HasAccess(authData.UserId, authData.OrganizationId, itemAccessLevel, cancellationToken);
        if (!hasAccess)
            throw new NotFoundException($"Organization: {authData.OrganizationId} is unavailable or permission: {itemAccessLevel} is missing");
    }

    public async Task HasAccessOrThrow(OrganizationAuthData authData, AdminAccessLevel accessLevel, CancellationToken cancellationToken)
    {
        var result = await GetAvailable(authData.UserId, (organizationUsers) =>
        {
            return organizationUsers
                .Where(ou => ou.OrganizationId == authData.OrganizationId)
                .AnyAsyncEF(ou => ou.AdminAccessLevel.HasFlag(accessLevel), cancellationToken);
        });
        
        if (!result)
            throw new NotFoundException($"Organization: {authData.OrganizationId} is unavailable or permission: {accessLevel} is missing");
    }

    private Task<bool> HasAccess(
        Guid userId,
        long organizationId,
        ItemAccessLevel itemAccessLevel,
        CancellationToken cancellationToken)
    {
        return GetAvailable(userId, organizations =>
        {
            return organizations
                .Where(x => x.OrganizationId == organizationId)
                .AnyAsyncEF(x => x.ItemAccessLevel.HasFlag(itemAccessLevel), cancellationToken);
        });
    }
}