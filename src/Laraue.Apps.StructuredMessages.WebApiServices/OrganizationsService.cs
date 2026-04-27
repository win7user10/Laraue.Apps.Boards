using System.ComponentModel.DataAnnotations;
using Laraue.Apps.StructuredMessages.DataAccess;
using Laraue.Apps.StructuredMessages.DataAccess.Enums;
using Laraue.Apps.StructuredMessages.DataAccess.Models;
using Laraue.Apps.StructuredMessages.Services;
using Laraue.Core.DataAccess.EFCore.Extensions;
using Laraue.Core.Exceptions.Web;
using LinqToDB.EntityFrameworkCore;

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
            organizationUsers => organizationUsers
                .OrderByDescending(x => x.Organization!.Type)
                .ThenBy(x => x.Organization!.Name)
                .Select(x => new OrganizationDto
                {
                    Id = x.Organization!.Id,
                    AccessLevel = x.AccessLevel,
                    Name = x.Organization.Name,
                    Color = x.Organization.Color,
                    SpacesCount = x.Organization.Spaces!.Count,
                    IsPersonal = x.Organization.Type == OrganizationType.Personal,
                    AdminAccessLevel = x.AdminAccessLevel,
                })
                .ToListAsyncEF(cancellationToken));

        return allOrganizations.ToArray();
    }

    public Task<OrganizationDto> GetOrganization(GetOrganizationRequest request, CancellationToken cancellationToken)
    {
        return organizationAccessService.GetAvailable(
            request.AuthData.UserId,
            organizations => organizations
                .Where(o => o.Id == request.AuthData.OrganizationId)
                .Select(x => new OrganizationDto
                {
                    Id = x.Organization!.Id,
                    AccessLevel = x.AccessLevel,
                    Name = x.Organization.Name,
                    Color = x.Organization.Color,
                    SpacesCount = x.Organization.Spaces!.Count,
                    IsPersonal = x.Organization.Type == OrganizationType.Personal,
                    AdminAccessLevel = x.AdminAccessLevel,
                })
                .FirstOrThrowNotFoundEFAsync($"Organization: {request.AuthData.OrganizationId} is not found", cancellationToken));
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
            throw new NotFoundException($"Organization is not found: {request.JoinCode}");
        
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

    public async Task SetUserPermissions(SetPermissionsRequest request, CancellationToken cancellationToken)
    {
        // TODO - check that passed permissions are correct, check access to passed items
        var organizationUser = await context.OrganizationUsers
            .Where(x => x.Id == request.OrganizationUserId)
            .Where(x => x.OrganizationId == request.AuthData.OrganizationId)
            .Select(x => new { x.OrganizationId })
            .FirstOrThrowNotFoundEFAsync($"OrganizationUser: {request.OrganizationUserId} is not found", cancellationToken);
        
        await organizationAccessService.HasAccessOrThrow(
            request.AuthData,
            AdminAccessLevel.ManagePermissions,
            cancellationToken);
        
        await coreOrganizationsService.SetUserPermissions(
            request.OrganizationUserId,
            request.UserPermissions,
            cancellationToken);
    }

    public async Task<UserPermissions> GetUserPermissions(GetUserPermissionsRequest request, CancellationToken cancellationToken)
    {
        await organizationAccessService.HasAccessOrThrow(
            request.AuthData,
            AdminAccessLevel.ManagePermissions,
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
        await organizationAccessService.HasAccessOrThrow(
            new OrganizationAuthData
            {
                OrganizationId = request.OrganizationId,
                UserId = request.UserId,
            },
            AccessLevel.None,
            cancellationToken);

        return authService.CreateOrganizationToken(request.OrganizationId, request.UserId);
    }

    public async Task<OrganizationMember[]> GetOrganizationMembers(
        GetOrganizationMembersRequest request,
        CancellationToken cancellationToken)
    {
        await organizationAccessService.HasAccessOrThrow(
            request.AuthData,
            AccessLevel.ReadItems,
            cancellationToken);

        var data = await context.OrganizationUsers
            .Where(o => o.OrganizationId == request.AuthData.OrganizationId)
            .Select(x => new OrganizationMember
            {
                Color = Palette.DefaultUserColor,
                FirstName = x.User!.TelegramFirstName,
                LastName = x.User.TelegramLastName,
                OrganizationUserId = x.Id,
                Username = x.User.TelegramUserName,
                Initials = null,
                IsOwner = x.Organization!.OwnerId == x.UserId,
                AccessLevel = x.AccessLevel,
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

    public async Task<PermittableSpace[]> GetPermittableEntities(
        GetPermittableEntitiesRequest request,
        CancellationToken cancellationToken)
    {
        await organizationAccessService.HasAccessOrThrow(
            request.AuthData,
            AdminAccessLevel.ManagePermissions,
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
    public required int SpacesCount { get; set; }
    public required AccessLevel AccessLevel { get; set; }
    public required AdminAccessLevel AdminAccessLevel { get; set; }
    public required bool IsPersonal { get; set; }
}

public record JoinOrganizationRequest
{
    public Guid UserId { get; set; }
    public required string JoinCode { get; set; }
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

public record OrganizationMember
{
    public long OrganizationUserId { get; set; }
    public string? Username { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public required string Color { get; set; }
    public string? Initials { get; set; }
    public required bool IsOwner { get; set; }
    public required AccessLevel AccessLevel { get; set; }
}

public record GetPermittableEntitiesRequest
{
    public required OrganizationAuthData AuthData { get; set; }
}