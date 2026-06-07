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
    
    Task CanCreateSpacesOrThrow(
        long organizationId,
        Guid userId,
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

    public async Task CanCreateSpacesOrThrow(
        long organizationId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var result = await GetAvailable(userId, (organizationUsers) =>
        {
            return organizationUsers
                .Where(ou => ou.OrganizationId == organizationId)
                .AnyAsyncEF(ou => ou.CanCreateSpaces, cancellationToken);
        });
        
        if (!result)
            throw new NotFoundException($"Organization: {organizationId} space creation is forbidden");
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
}