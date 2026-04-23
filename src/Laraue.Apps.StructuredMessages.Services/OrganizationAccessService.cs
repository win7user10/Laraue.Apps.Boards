using Laraue.Apps.StructuredMessages.DataAccess;
using Laraue.Apps.StructuredMessages.DataAccess.Enums;
using Laraue.Apps.StructuredMessages.DataAccess.Models;
using Laraue.Core.Exceptions.Web;
using Microsoft.EntityFrameworkCore;

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
            throw new NotFoundException();
    }
    
    private Task<bool> HasAccess(
        Guid userId,
        long organizationId,
        AccessLevel accessLevel,
        CancellationToken cancellationToken)
    {
        if (organizationId == IdService.NullId)
            return Task.FromResult(accessLevel <= AccessLevel.Read);
        
        return GetAvailable(userId, organizations =>
        {
            return organizations
                .Where(x => x.OrganizationId == organizationId)
                .AnyAsync(x => x.AccessLevel >= accessLevel, cancellationToken);
        });
    }
}