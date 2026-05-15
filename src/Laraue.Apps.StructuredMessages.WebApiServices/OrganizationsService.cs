using System.ComponentModel.DataAnnotations;
using Laraue.Apps.StructuredMessages.DataAccess;
using Laraue.Apps.StructuredMessages.DataAccess.Enums;
using Laraue.Apps.StructuredMessages.DataAccess.Models;
using Laraue.Apps.StructuredMessages.Services;
using Laraue.Core.DataAccess.EFCore.Extensions;
using Laraue.Core.DataAccess.Linq2DB.Extensions;
using Laraue.Core.Exceptions.Web;
using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Laraue.Apps.StructuredMessages.WebApiServices;

public interface IOrganizationsService
{
    Task<OrganizationDto[]> GetOrganizations(
        GetOrganizationsRequest request,
        CancellationToken cancellationToken);
    
    Task<OrganizationDto> GetOrganization(
        GetOrganizationRequest request,
        CancellationToken cancellationToken);
    
    Task<long> Create(
        CreateOrganizationRequest request,
        CancellationToken cancellationToken);
    
    Task Update(
        EditOrganizationRequest request,
        CancellationToken cancellationToken);
    
    Task Delete(
        DeleteOrganizationRequest request,
        CancellationToken cancellationToken);
    
    Task Join(
        JoinOrganizationRequest request,
        CancellationToken cancellationToken);
    
    Task Leave(
        LeaveOrganizationRequest request,
        CancellationToken cancellationToken);
    
    Task RevokeAccess(
        RevokeAccessRequest request,
        CancellationToken cancellationToken);
    
    Task<string> RegenerateJoinCode(
        RegenerateJoinCodeRequest request,
        CancellationToken cancellationToken);
    
    Task SetUserPermissions(
        SetPermissionsRequest request,
        CancellationToken cancellationToken);
    
    Task<UserPermissions> GetUserPermissions(
        GetUserPermissionsRequest request,
        CancellationToken cancellationToken);
    
    Task<string> Login(
        LoginRequest request,
        CancellationToken cancellationToken);
    
    Task<OrganizationMember[]> GetOrganizationMembers(
        GetOrganizationMembersRequest request,
        CancellationToken cancellationToken);
    
    Task<string?> GetOrganizationJoinCode(
        GetOrganizationJoinCodeRequest request,
        CancellationToken cancellationToken);
    
    Task<PermittableSpace[]> GetPermittableEntities(
        GetPermittableEntitiesRequest request,
        CancellationToken cancellationToken);
}

public class OrganizationsService(
    ICoreOrganizationsService coreOrganizationsService,
    DatabaseContext context,
    IAuthService authService,
    IOrganizationAccessService organizationAccessService)
    : IOrganizationsService
{
    public async Task<OrganizationDto[]> GetOrganizations(
        GetOrganizationsRequest request,
        CancellationToken cancellationToken)
    {
        var allOrganizations = await organizationAccessService.GetAvailable(
            request.UserId,
            organizationUsers => Project(organizationUsers
                .OrderByDescending(x => x.Organization!.Type)
                .ThenBy(x => x.Organization!.Name))
                .ToListAsyncEF(cancellationToken));

        return allOrganizations.ToArray();
    }

    public Task<OrganizationDto> GetOrganization(GetOrganizationRequest request, CancellationToken cancellationToken)
    {
        return organizationAccessService.GetAvailable(
            request.AuthData.UserId,
            organizations => Project(organizations
                .Where(o => o.OrganizationId == request.AuthData.OrganizationId))
                .FirstOrThrowNotFoundEFAsync($"Organization: {request.AuthData.OrganizationId} is not found", cancellationToken));
    }

    private IQueryable<OrganizationDto> Project(IQueryable<OrganizationUser> query)
    {
        return query
            .Select(x => new OrganizationDto
            {
                Id = x.Organization!.Id,
                CanCreateSpaces = x.SpacesAccessLevel.HasFlag(ChildrenAccessLevel.Create),
                CanUpdate = x.AdminAccessLevel.HasFlag(AdminAccessLevel.UpdateOrganization),
                CanDelete = x.Organization.Type != OrganizationType.Personal &&
                            x.AdminAccessLevel.HasFlag(AdminAccessLevel.DeleteOrganization),
                Name = x.Organization.Name,
                Color = x.Organization.Color,
                IsPersonal = x.Organization.Type == OrganizationType.Personal,
                CanManage = x.AdminAccessLevel.HasFlag(AdminAccessLevel.Manage),
            });
    }

    public Task<long> Create(CreateOrganizationRequest request, CancellationToken cancellationToken)
    {
        return coreOrganizationsService.Create(
            request.UserId,
            request.Name,
            request.Color,
            cancellationToken);
    }

    public async Task Update(EditOrganizationRequest request, CancellationToken cancellationToken)
    {
        await organizationAccessService.HasAccessOrThrow(
            new OrganizationAuthData
            {
                OrganizationId = request.Id,
                UserId = request.UserId,
            },
            AdminAccessLevel.UpdateOrganization,
            cancellationToken);

        await coreOrganizationsService.Update(
            request.Id,
            setters => setters
                .SetProperty(x => x.Color, request.Color)
                .SetProperty(x => x.Name, request.Name),
            cancellationToken);
    }

    public async Task Delete(DeleteOrganizationRequest request, CancellationToken cancellationToken)
    {
        await organizationAccessService.HasAccessOrThrow(
            new OrganizationAuthData
            {
                OrganizationId = request.Id,
                UserId = request.UserId,
            },
            AdminAccessLevel.DeleteOrganization,
            cancellationToken);
        
        await coreOrganizationsService.Delete(request.Id, cancellationToken);
    }

    public async Task Join(JoinOrganizationRequest request, CancellationToken cancellationToken)
    {
        var organizationId = await coreOrganizationsService.GetOrganizationIdByJoinCode(
            request.JoinCode,
            cancellationToken);
        
        if (organizationId == null)
            throw new NotFoundException($"Organization code: {request.JoinCode} is not found");
        
        if (await coreOrganizationsService.HasMember(
            organizationId.Value,
            request.UserId,
            cancellationToken))
            throw new NotAcceptableException("User is already member of this organization");

        await coreOrganizationsService.AddMember(
            organizationId.Value,
            request.UserId,
            cancellationToken);
    }

    public async Task Leave(LeaveOrganizationRequest request, CancellationToken cancellationToken)
    {
        await context.OrganizationUsers
            .Where(x => x.UserId == request.UserId)
            .Where(x => x.OrganizationId == request.OrganizationId)
            .DeleteOrThrowNotFoundLinq2DbAsync(
                "Organization is not found or user is not a participator of organization", 
                cancellationToken);
    }

    public async Task RevokeAccess(RevokeAccessRequest request, CancellationToken cancellationToken)
    {
        await organizationAccessService.HasAccessOrThrow(
            request.AuthData,
            AdminAccessLevel.Manage,
            cancellationToken);
        
        var userData = await context.OrganizationUsers
            .Where(x => x.Id == request.OrganizationUserId)
            .Select(x => new
            {
                IsOwner = x.Organization!.OwnerId == x.UserId,
            })
            .FirstOrThrowNotFoundEFAsync("User is not found in organization", cancellationToken);

        if (userData.IsOwner)
            throw new ForbiddenException("Owner access can't be revoked");

        await context.OrganizationUsers
            .Where(x => x.Id == request.OrganizationUserId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<string> RegenerateJoinCode(RegenerateJoinCodeRequest request, CancellationToken cancellationToken)
    {
        await organizationAccessService.HasAccessOrThrow(
            request.AuthData,
            AdminAccessLevel.Manage,
            cancellationToken);

        var newCode = StringGenerator.GenerateJoinCode();
        await context.Organizations
            .Where(x => x.Id == request.AuthData.OrganizationId)
            .ExecuteUpdateAsync(u => u
                    .SetProperty(p => p.JoinCode, newCode),
                cancellationToken);
        
        return newCode;
    }

    public async Task SetUserPermissions(SetPermissionsRequest request, CancellationToken cancellationToken)
    {
        await organizationAccessService.HasAccessOrThrow(
            request.AuthData,
            AdminAccessLevel.Manage,
            cancellationToken);
        
        await context.OrganizationUsers
            .Where(x => x.Id == request.OrganizationUserId)
            .AnyOrThrowNotFoundEFAsync(
                x => x.OrganizationId == request.AuthData.OrganizationId, 
                $"OrganizationUser: {request.OrganizationUserId} is not found", cancellationToken);
        
        // Check that passed spaces belongs to organization
        if (request.UserPermissions.Direct.Count > 0)
        {
            var permittableEntities = (await coreOrganizationsService.GetPermittableEntities(
                request.AuthData.OrganizationId,
                cancellationToken))
                .ToDictionary(
                    x => x.Id,
                    x => new { Self = x, Epics = x.Epics.ToDictionary(e => e.Id) });

            var errors = new List<string>();
            
            foreach (var directSpacePermission in request.UserPermissions.Direct)
            {
                if (!permittableEntities.TryGetValue(directSpacePermission.Key, out var space))
                {
                    errors.Add($"Space: '{directSpacePermission.Key}'. Entity is not found");
                    continue;
                }
                if (space.Self.IsDefault && directSpacePermission.Value.Self.HasFlag(EntityAccessLevel.Delete))
                    errors.Add($"Space: '{directSpacePermission.Key}'. Attempt to add delete permission to Default space");

                foreach (var directEpicPermission in directSpacePermission.Value.DirectEpics)
                {
                    if (!space.Epics.TryGetValue(directEpicPermission.Key, out var epic))
                        errors.Add($"Space: '{directSpacePermission.Key}', Epic: '{directEpicPermission.Key}'. Entity is not found");
                    else if (epic.IsDefault && directEpicPermission.Value.Self.HasFlag(EntityAccessLevel.Delete))
                        errors.Add($"Space: '{directSpacePermission.Key}', Epic: '{directEpicPermission.Key}'. Attempt to add delete permission to Default epic");
                }
            }

            if (errors.Count != 0)
            {
                throw new BadRequestException(
                    new Dictionary<string, string?[]>
                    {
                        [nameof(UserPermissions.Direct)] = errors.ToArray(),
                    });
            }
        }

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        
        await coreOrganizationsService.SetUserPermissions(
            request.OrganizationUserId,
            request.UserPermissions,
            cancellationToken);
        
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<UserPermissions> GetUserPermissions(GetUserPermissionsRequest request, CancellationToken cancellationToken)
    {
        await organizationAccessService.HasAccessOrThrow(
            request.AuthData,
            AdminAccessLevel.Manage,
            cancellationToken);
        
        await context.OrganizationUsers
            .Where(x => x.Id == request.OrganizationUserId)
            .AnyOrThrowNotFoundEFAsync(
                x => x.OrganizationId == request.AuthData.OrganizationId,
                $"OrganizationUser: {request.OrganizationUserId} is not found", cancellationToken);
        
        return await coreOrganizationsService.GetUserPermissions(
            request.OrganizationUserId,
            cancellationToken);
    }

    public async Task<string> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        await organizationAccessService.GetAvailable(
            request.UserId,
            organizations => organizations
                .Where(o => o.UserId == request.UserId)
                .FirstOrThrowNotFoundEFAsync(
                    "Organization is not exists or user does not belong to organization",
                    cancellationToken));

        return authService.CreateOrganizationToken(request.OrganizationId, request.UserId);
    }

    public async Task<OrganizationMember[]> GetOrganizationMembers(
        GetOrganizationMembersRequest request,
        CancellationToken cancellationToken)
    {
        await organizationAccessService.HasAccessOrThrow(
            request.AuthData,
            AdminAccessLevel.Manage,
            cancellationToken);

        var data = await context.OrganizationUsers
            .Where(o => o.OrganizationId == request.AuthData.OrganizationId)
            .Select(x => new OrganizationMember
            {
                Color = x.User!.Color,
                FirstName = x.User.TelegramFirstName,
                LastName = x.User.TelegramLastName,
                OrganizationUserId = x.Id,
                Username = x.User.TelegramUserName,
                Initials = null,
                IsOwner = x.Organization!.OwnerId == x.UserId,
                AdminAccessLevel = x.AdminAccessLevel,
            })
            .ToArrayAsyncEF(cancellationToken);

        foreach (var item in data)
        {
            item.Initials = UserInitialsUtility.GetInitials(
                item.Username,
                item.FirstName,
                item.LastName).Initial;
        }
        
        return data;
    }

    public async Task<string?> GetOrganizationJoinCode(GetOrganizationJoinCodeRequest request, CancellationToken cancellationToken)
    {
        await organizationAccessService.HasAccessOrThrow(
            request.AuthData,
            AdminAccessLevel.Manage,
            cancellationToken);

        return await context.Organizations
            .Where(o => o.Id == request.AuthData.OrganizationId)
            .Select(x => x.JoinCode)
            .FirstOrDefaultAsyncEF(cancellationToken);
    }

    public async Task<PermittableSpace[]> GetPermittableEntities(
        GetPermittableEntitiesRequest request,
        CancellationToken cancellationToken)
    {
        await organizationAccessService.HasAccessOrThrow(
            request.AuthData,
            AdminAccessLevel.Manage,
            cancellationToken);

        return await coreOrganizationsService.GetPermittableEntities(
            request.AuthData.OrganizationId,
            cancellationToken);
    }
}

public record CreateOrganizationRequest
{
    public Guid UserId { get; set; }
    
    [MaxLength(128)]
    public required string Name { get; set; }
    
    [MaxLength(7)]
    [MinLength(7)]
    public required string Color { get; set; }
}

public record EditOrganizationRequest
{
    public long Id { get; set; }
    public Guid UserId { get; set; }
    
    [MaxLength(128)]
    public required string Name { get; set; }
    
    [MaxLength(7)]
    [MinLength(7)]
    public required string Color { get; set; }
}

public record DeleteOrganizationRequest
{
    public long Id { get; set; }
    public Guid UserId { get; set; }
}

public record GetOrganizationsRequest
{
    public Guid UserId { get; set; }
}

public record GetOrganizationRequest
{
    public required OrganizationAuthData AuthData { get; set; }
}

public record OrganizationDto
{
    public long Id { get; set; }
    public required string Name { get; set; }
    public required string? Color { get; set; }
    public required bool CanCreateSpaces { get; set; }
    public required bool CanUpdate { get; set; }
    public required bool CanDelete { get; set; }
    public required bool CanManage { get; set; }
    public required bool IsPersonal { get; set; }
}

public record JoinOrganizationRequest
{
    public Guid UserId { get; set; }
    public required string JoinCode { get; set; }
}

public record RevokeAccessRequest
{
    public OrganizationAuthData AuthData { get; set; } = new();
    public long OrganizationUserId { get; set; }
}

public record RegenerateJoinCodeRequest
{
    public OrganizationAuthData AuthData { get; set; } = new();
}

public record LeaveOrganizationRequest
{
    public Guid UserId { get; set; }
    public long OrganizationId { get; set; }
}

public record SetPermissionsRequest
{
    public OrganizationAuthData AuthData { get; set; } = new();
    public long OrganizationUserId { get; set; }
    public required UserPermissions UserPermissions { get; set; }
}

public record GetUserPermissionsRequest
{
    public OrganizationAuthData AuthData { get; set; } = new ();
    public long OrganizationUserId { get; set; }
}

public record LoginRequest
{
    public Guid UserId { get; set; }
    public long OrganizationId { get; set; }
}

public record GetOrganizationMembersRequest
{
    public required OrganizationAuthData AuthData { get; set; }
}

public record GetOrganizationJoinCodeRequest
{
    public required OrganizationAuthData AuthData { get; set; }
}

public record OrganizationMember
{
    public long OrganizationUserId { get; set; }
    public string? Username { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public required string Color { get; set; }
    public string? Initials { get; set; }
    public required bool IsOwner { get; set; }
    public required AdminAccessLevel AdminAccessLevel { get; set; }
}

public record GetPermittableEntitiesRequest
{
    public required OrganizationAuthData AuthData { get; set; }
}