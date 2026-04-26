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
        Guid userId,
        long organizationId,
        AccessLevel accessLevel,
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
        Guid userId,
        long organizationId,
        AccessLevel accessLevel,
        CancellationToken cancellationToken)
    {
        var hasAccess = await HasAccess(userId, organizationId, accessLevel, cancellationToken);
        if (!hasAccess)
            throw new NotFoundException($"Organization: {organizationId} or permissions: {accessLevel} is not allowed");
    }
    
    private Task<bool> HasAccess(
        Guid userId,
        long organizationId,
        AccessLevel accessLevel,
        CancellationToken cancellationToken)
    {
        return GetAvailable(userId, organizations =>
        {
            return organizations
                .Where(x => x.OrganizationId == organizationId)
                .AnyAsyncEF(x => x.AccessLevel.HasFlag(accessLevel), cancellationToken);
        });
    }
}