using Laraue.Apps.StructuredMessages.DataAccess;
using Laraue.Apps.StructuredMessages.DataAccess.Models;
using Laraue.Core.DateTime.Services.Abstractions;
using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace Laraue.Apps.StructuredMessages.Services;

public interface ICoreOrganizationsService
{
    Task<long> Create(
        Guid ownerId,
        string name,
        string color,
        CancellationToken cancellationToken);
    
    Task Update(
        long id,
        Action<UpdateSettersBuilder<Organization>> setters,
        CancellationToken cancellationToken);
    
    Task Delete(
        long id,
        CancellationToken cancellationToken);
    
    Task<bool> HasAccess(
        long organizationId,
        Guid userId,
        AccessType accessType,
        CancellationToken cancellationToken);
    
    Task AddMember(
        long organizationId,
        Guid userId,
        CancellationToken cancellationToken);
    
    Task<long?> GetOrganizationIdByJoinCode(
        string code,
        CancellationToken cancellationToken);
}

public class CoreOrganizationsService(
    DatabaseContext context,
    IDateTimeProvider dateTimeProvider)
    : ICoreOrganizationsService
{
    public async Task<long> Create(
        Guid ownerId,
        string name,
        string color,
        CancellationToken cancellationToken)
    {
        var dateTime = dateTimeProvider.UtcNow;
        
        var entity = new Organization
        {
            OwnerId = ownerId,
            Name = name,
            Color = color,
            CreatedAt = dateTime,
            UpdatedAt = dateTime,
            JoinCode = StringGenerator.GenerateJoinCode(),
        };
        
        context.Organizations.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        
        return entity.Id;
    }

    public Task Update(long id, Action<UpdateSettersBuilder<Organization>> setters, CancellationToken cancellationToken)
    {
        var date = dateTimeProvider.UtcNow;
        
        return context.Organizations
            .Where(x => x.Id == id)
            .ExecuteUpdateAsync(
                update =>
                {
                    setters(update);
                    update
                        .SetProperty(p => p.UpdatedAt, date);
                },
                cancellationToken);
    }

    public Task Delete(long id, CancellationToken cancellationToken)
    {
        return context.Organizations
            .Where(c => c.Id == id)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public Task<bool> HasAccess(
        long organizationId,
        Guid userId,
        AccessType accessType,
        CancellationToken cancellationToken)
    {
        return context.Organizations
            .Where(x => x.OwnerId == userId)
            .Where(x => x.Id == organizationId)
            .AnyAsyncEF(cancellationToken);
    }

    public Task AddMember(long organizationId, Guid userId, CancellationToken cancellationToken)
    {
        context.OrganizationUsers.Add(new OrganizationUser
        {
            OrganizationId = organizationId,
            UserId = userId,
        });

        return context.SaveChangesAsync(cancellationToken);
    }

    public async Task<long?> GetOrganizationIdByJoinCode(string code, CancellationToken cancellationToken)
    {
        return (await context.Organizations
            .Where(x => x.JoinCode == code)
            .Select(x => new { x.Id })
            .FirstOrDefaultAsyncEF(cancellationToken))?.Id;
    }
}