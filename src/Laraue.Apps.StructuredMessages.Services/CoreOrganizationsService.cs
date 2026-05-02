using Laraue.Apps.StructuredMessages.DataAccess;
using Laraue.Apps.StructuredMessages.DataAccess.Enums;
using Laraue.Apps.StructuredMessages.DataAccess.Models;
using Laraue.Core.DataAccess.EFCore.Extensions;
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
    
    Task<bool> HasMember(
        long organizationId,
        Guid userId,
        CancellationToken cancellationToken);
    
    Task<long> AddMember(
        long organizationId,
        Guid userId,
        CancellationToken cancellationToken);
    
    Task<long?> GetOrganizationIdByJoinCode(
        string code,
        CancellationToken cancellationToken);
    
    Task SetUserPermissions(
        long organizationUserId,
        UserPermissions userPermissions,
        CancellationToken cancellationToken);

    Task<UserPermissions> GetUserPermissions(
        long organizationUserId,
        CancellationToken cancellationToken);

    Task<PermittableSpace[]> GetPermittableEntities(
        long organizationId,
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
        var timestamp = dateTimeProvider.UtcNow;

        var entity = OrganizationDefaults.GetNewOrganizationEntity(
            ownerId,
            name,
            color,
            timestamp,
            OrganizationType.Organization);

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

    public async Task Delete(long id, CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        var spaceIds = context.Spaces
            .Where(c => c.OrganizationId == id)
            .Select(x => (long?)x.Id);

        await context.Epics
            .Where(x => spaceIds.Contains(x.SpaceId))
            .ExecuteDeleteAsync(cancellationToken);

        await context.Spaces
            .Where(c => c.OrganizationId == id)
            .ExecuteDeleteAsync(cancellationToken);

        await context.Organizations
            .Where(c => c.Id == id)
            .ExecuteDeleteAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    public Task<bool> HasMember(long organizationId, Guid userId, CancellationToken cancellationToken)
    {
        return context.OrganizationUsers
            .Where(x => x.OrganizationId == organizationId)
            .Where(x => x.UserId == userId)
            .AnyAsyncEF(cancellationToken);
    }

    public async Task<long> AddMember(long organizationId, Guid userId, CancellationToken cancellationToken)
    {
        var user = new OrganizationUser
        {
            OrganizationId = organizationId,
            UserId = userId,
        };
        
        context.OrganizationUsers.Add(user);

        await context.SaveChangesAsync(cancellationToken);

        return user.Id;
    }

    public async Task<long?> GetOrganizationIdByJoinCode(string code, CancellationToken cancellationToken)
    {
        return (await context.Organizations
            .Where(x => x.JoinCode == code)
            .Select(x => new { x.Id })
            .FirstOrDefaultAsyncEF(cancellationToken))?.Id;
    }
    
    public async Task SetUserPermissions(
        long organizationUserId,
        UserPermissions userPermissions,
        CancellationToken cancellationToken)
    {
        NormalizePermissions(userPermissions);
        
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        
        // Remove old permissions
        await context.DirectEpicPermissions
            .Where(x => x.OrganizationUserId == organizationUserId)
            .ExecuteDeleteAsync(cancellationToken);
        
        await context.DirectSpacePermissions
            .Where(x => x.OrganizationUserId == organizationUserId)
            .ExecuteDeleteAsync(cancellationToken);
        
        // Set organization permissions
        await context.OrganizationUsers
            .Where(x => x.Id == organizationUserId)
            .ExecuteUpdateAsync(x => x
                .SetProperty(p => p.AdminAccessLevel, userPermissions.Administrative)
                .SetProperty(p => p.SpacesAccessLevel, userPermissions.GlobalAccessLevels.Spaces)
                .SetProperty(p => p.EpicsAccessLevel, userPermissions.GlobalAccessLevels.Epics)
                .SetProperty(p => p.IssuesAccessLevel, userPermissions.GlobalAccessLevels.Issues),
                cancellationToken);

        // Set spaces direct permissions
        foreach (var spaceLevel in userPermissions.DirectAccessLevels)
        {
            var spacePermission = new DirectSpacePermission
            {
                OrganizationUserId = organizationUserId,
                SpaceId = spaceLevel.Key,
                EntityAccessLevel = spaceLevel.Value.Self,
                ChildrenEpicsAccessLevel = spaceLevel.Value.Epics,
                ChildrenIssuesAccessLevel = spaceLevel.Value.Issues,
            };

            context.DirectSpacePermissions.Add(spacePermission);
            
            // Set epics direct permissions
            foreach (var epicLevel in spaceLevel.Value.DirectEpics)
            {
                var epicPermission = new DirectEpicPermission
                {
                    OrganizationUserId = organizationUserId,
                    EpicId = epicLevel.Key,
                    EntityAccessLevel = epicLevel.Value.Self,
                    ChildrenIssuesAccessLevel = epicLevel.Value.Issues,
                };

                context.DirectEpicPermissions.Add(epicPermission);
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<UserPermissions> GetUserPermissions(long organizationUserId, CancellationToken cancellationToken)
    {
        var organizationAccessLevel = await context.OrganizationUsers
            .Where(x => x.Id == organizationUserId)
            .Select(x => new
            {
                x.SpacesAccessLevel,
                x.EpicsAccessLevel,
                x.IssuesAccessLevel,
                x.AdminAccessLevel,
            })
            .FirstOrThrowNotFoundEFAsync($"User: {organizationUserId} is not found in organization", cancellationToken);

        var spacePermissions = await context.DirectSpacePermissions
            .Where(x => x.OrganizationUserId == organizationUserId)
            .Select(x => new
            {
                x.SpaceId,
                x.ChildrenEpicsAccessLevel,
                x.ChildrenIssuesAccessLevel,
                x.EntityAccessLevel,
            })
            .ToArrayAsyncEF(cancellationToken);

        var epicPermissions = await context.DirectEpicPermissions
            .Where(x => x.OrganizationUserId == organizationUserId)
            .Select(x => new
            {
                x.EpicId,
                x.ChildrenIssuesAccessLevel,
                x.EntityAccessLevel,
                x.Epic!.SpaceId
            })
            .ToArrayAsyncEF(cancellationToken);

        var epicAccessLevels = epicPermissions
            .GroupBy(
                x => x.SpaceId,
                x => x)
            .ToDictionary(x => x.Key, x => x
                .ToDictionary(
                    y => y.EpicId,
                    y => new DirectEpicAccessLevel
                    {
                        Issues = y.ChildrenIssuesAccessLevel,
                        Self = y.EntityAccessLevel,
                    }));

        var spaceAccessLevels = spacePermissions.ToDictionary(
            x => x.SpaceId,
            x => new DirectSpaceAccessLevel
            {
                Epics = x.ChildrenEpicsAccessLevel,
                Issues = x.ChildrenIssuesAccessLevel, 
                Self = x.EntityAccessLevel,
                DirectEpics = epicAccessLevels[x.SpaceId]
            });

        var permissions = new UserPermissions
        {
            Administrative = organizationAccessLevel.AdminAccessLevel,
            GlobalAccessLevels = new GlobalAccessLevels
            {
                Epics = organizationAccessLevel.EpicsAccessLevel,
                Spaces = organizationAccessLevel.SpacesAccessLevel,
                Issues = organizationAccessLevel.IssuesAccessLevel,
            },
            DirectAccessLevels = spaceAccessLevels
        };
        
        NormalizePermissions(permissions);

        return permissions;
    }

    public async Task<PermittableSpace[]> GetPermittableEntities(
        long organizationId,
        CancellationToken cancellationToken)
    {
        var spaces = await context.Spaces
            .Where(x => x.OrganizationId == organizationId)
            .ToDictionaryAsyncEF(x => x.Id, x => x.Name, cancellationToken);
        
        var epics = await context.Epics
            .Where(x => x.Space!.OrganizationId == organizationId)
            .Select(x => new { x.Id, x.Name, x.SpaceId })
            .ToArrayAsyncEF(cancellationToken);
        
        var epicsBySpaces = epics
            .GroupBy(x => x.SpaceId)
            .ToDictionary(x => x.Key, x => x
                .ToDictionary(y => y.Id, y => y.Name));

        return spaces
            .Select(s => new PermittableSpace(
                s.Key,
                s.Value,
                epicsBySpaces[s.Key]))
            .ToArray();
    }

    /// <summary>
    /// Add explicit implicitly added permissions, e.g. Read is always need when Write is active.
    /// </summary>
    private void NormalizePermissions(UserPermissions permissions)
    {
        // When at the bottom level something is active, it provides read access to the top level.
        // When user can view Issues he should view Epic where these Issues are situated.
        
        // Global access setup
        var global = permissions.GlobalAccessLevels;
        
        if (global.Issues > ChildrenAccessLevel.None)
        {
            global.Spaces |= ChildrenAccessLevel.Read;
            global.Epics |= ChildrenAccessLevel.Read;
            global.Issues |= ChildrenAccessLevel.Read;
        }

        if (global.Epics > ChildrenAccessLevel.None)
        {
            global.Spaces |= ChildrenAccessLevel.Read;
            global.Epics |= ChildrenAccessLevel.Read;
        }
        
        if (global.Spaces > ChildrenAccessLevel.None)
            global.Spaces |= ChildrenAccessLevel.Read;
        
        // Direct access setup
        foreach (var directSpaceAccess in permissions.DirectAccessLevels)
        {
            foreach (var directEpicAccess in directSpaceAccess.Value.DirectEpics)
            {
                if (directEpicAccess.Value.Issues > ChildrenAccessLevel.None ||
                    directEpicAccess.Value.Self > EntityAccessLevel.None)
                {
                    directSpaceAccess.Value.Self |= EntityAccessLevel.Read;
                    directEpicAccess.Value.Self |= EntityAccessLevel.Read;
                }
            }
            
            if (directSpaceAccess.Value.Issues > ChildrenAccessLevel.None || directSpaceAccess.Value.Self > EntityAccessLevel.None)
                directSpaceAccess.Value.Self |= EntityAccessLevel.Read;
        }
    }
}

public record UserPermissions
{
    public GlobalAccessLevels GlobalAccessLevels { get; set; } = new();
    public Dictionary<long, DirectSpaceAccessLevel> DirectAccessLevels { get; set; } = new();
    public AdminAccessLevel Administrative { get; set; }
}

public record GlobalAccessLevels
{
    public ChildrenAccessLevel Spaces { get; set; }
    public ChildrenAccessLevel Epics { get; set; }
    public ChildrenAccessLevel Issues { get; set; }
}

public record DirectSpaceAccessLevel
{
    public ChildrenAccessLevel Epics { get; set; }
    public ChildrenAccessLevel Issues { get; set; }
    public EntityAccessLevel Self { get; set; }
    public Dictionary<long, DirectEpicAccessLevel> DirectEpics { get; set; } = new();
}

public record DirectEpicAccessLevel
{
    public ChildrenAccessLevel Issues { get; set; }
    public EntityAccessLevel Self { get; set; }
}

public record PermittableSpace(long Id, string Name, Dictionary<long, string> Epics);