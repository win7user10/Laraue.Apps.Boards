using Laraue.Apps.StructuredMessages.DataAccess;
using Laraue.Apps.StructuredMessages.DataAccess.Enums;
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
        AccessLevel accessLevel,
        CancellationToken cancellationToken);
    
    Task AddMember(
        long organizationId,
        Guid userId,
        CancellationToken cancellationToken);
    
    Task<long?> GetOrganizationIdByJoinCode(
        string code,
        CancellationToken cancellationToken);
    
    Task SetPermissions(
        long organizationUserId,
        Permissions permissions,
        CancellationToken cancellationToken);
    
    Task<Permissions> GetPermissions(
        long organizationUserId,
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
        AccessLevel accessLevel,
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

    public async Task SetPermissions(
        long organizationUserId,
        Permissions permissions,
        CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        // Set organization permissions
        await context.OrganizationUsers
            .Where(x => x.Id == organizationUserId)
            .ExecuteUpdateAsync(x => x
                .SetProperty(p => p.AccessLevel, permissions.OrganizationAccessLevel),
                cancellationToken);

        // Set spaces permissions
        await context.SpaceOrganizationUsers
            .Where(x => x.OrganizationUserId == organizationUserId)
            .ExecuteDeleteAsync(cancellationToken);
        
        var spaceLevels = permissions.SpacesAccessLevels;
        if (spaceLevels.DirectAccess is not null)
        {
            var spacePermissions = spaceLevels.DirectAccess
                .Select(x => new SpaceOrganizationUser
                {
                    AccessLevel = x.Value,
                    OrganizationUserId = organizationUserId,
                    SpaceId = x.Key,
                });
            
            context.SpaceOrganizationUsers.AddRange(spacePermissions);
        }
        else 
        {
            var spacePermission = new SpaceOrganizationUser
            {
                AccessLevel = spaceLevels.AccessLevel,
                OrganizationUserId = organizationUserId,
                SpaceId = null,
            };
            
            context.SpaceOrganizationUsers.Add(spacePermission);
        }
        
        // Set epics permissions
        await context.EpicOrganizationUsers
            .Where(x => x.OrganizationUserId == organizationUserId)
            .ExecuteDeleteAsync(cancellationToken);
        
        var epicLevels = permissions.EpicsAccessLevels;
        if (epicLevels.DirectAccess is not null)
        {
            var epicPermissions = epicLevels.DirectAccess
                .Select(x => new EpicOrganizationUser
                {
                    AccessLevel = x.Value,
                    OrganizationUserId = organizationUserId,
                    EpicId = x.Key,
                });
            
            context.EpicOrganizationUsers.AddRange(epicPermissions);
        }
        else 
        {
            var epicPermission = new EpicOrganizationUser
            {
                AccessLevel = epicLevels.AccessLevel,
                OrganizationUserId = organizationUserId,
                EpicId = null,
            };
            
            context.EpicOrganizationUsers.Add(epicPermission);
        }
        
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<Permissions> GetPermissions(long organizationUserId, CancellationToken cancellationToken)
    {
        var organizationAccessLevel = await context.OrganizationUsers
            .Where(x => x.Id == organizationUserId)
            .Select(x => x.AccessLevel)
            .FirstOrDefaultAsyncEF(cancellationToken);
        
        var spacePermissions = await context.SpaceOrganizationUsers
            .Where(x => x.Id == organizationUserId)
            .Select(x => new { x.SpaceId, x.AccessLevel })
            .ToArrayAsyncEF(cancellationToken);
        
        var epicPermissions = await context.EpicOrganizationUsers
            .Where(x => x.Id == organizationUserId)
            .Select(x => new { x.EpicId, x.AccessLevel })
            .ToArrayAsyncEF(cancellationToken);

        var spaceAccessLevels = ToAccessLevels(
            spacePermissions,
            x => x.SpaceId,
            x => x.AccessLevel);
        
        var epicAccessLevels = ToAccessLevels(
            epicPermissions,
            x => x.EpicId,
            x => x.AccessLevel);
        
        return new Permissions
        {
            OrganizationAccessLevel = organizationAccessLevel,
            SpacesAccessLevels = spaceAccessLevels,
            EpicsAccessLevels = epicAccessLevels,
        };
    }

    private static AccessLevels ToAccessLevels<T>(T[] permissions, Func<T, long?> getItemId, Func<T, AccessLevel> getAccessLevel)
    {
        if (permissions.Length == 0)
            return new AccessLevels();
        
        if (permissions.Length == 1 && getItemId(permissions[0]) is null)
            return new AccessLevels { AccessLevel = getAccessLevel(permissions[0]) };

        return new AccessLevels
        {
            DirectAccess = permissions
                .ToDictionary(x => getItemId(x)!.Value, getAccessLevel),
        };
    }
}

public record Permissions
{
    public AccessLevel OrganizationAccessLevel { get; set; }
    public required AccessLevels SpacesAccessLevels { get; set; }
    public required AccessLevels EpicsAccessLevels { get; set; }
}

/// <summary>
/// Allows to set up AccessLevel for all items in a time or separately for each item.
/// </summary>
public record AccessLevels
{
    public AccessLevel AccessLevel { get; init; }
    public Dictionary<long, AccessLevel>? DirectAccess { get; init; }
}