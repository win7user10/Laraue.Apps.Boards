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
            organizations => organizations
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
                })
                .ToListAsyncEF(cancellationToken));

        return allOrganizations.ToArray();
    }

    public async Task<OrganizationDto> GetOrganization(GetOrganizationRequest request, CancellationToken cancellationToken)
    {
        return await context.Organizations
            .Where(o => o.Id == request.AuthData.OrganizationId)
            .Select(x => new OrganizationDto
            {
                Id = x.Id,
                AccessLevel = x.Users!.FirstOrDefault(u => u.UserId == request.AuthData.UserId) != null
                    ? x.Users!.FirstOrDefault(u => u.UserId == request.AuthData.UserId)!.AccessLevel
                    : AccessLevel.Manage,
                Name = x.Name,
                Color = x.Color,
                SpacesCount = x.Spaces!.Count,
                IsPersonal = x.Type == OrganizationType.Personal,
            })
            .FirstOrThrowNotFoundEFAsync($"Organization: {request.AuthData.OrganizationId} is not found", cancellationToken);
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
            request.UserId,
            request.Id,
            AccessLevel.Manage,
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
            request.UserId,
            request.Id,
            AccessLevel.Manage,
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
            .Select(x => new { x.OrganizationId })
            .FirstOrThrowNotFoundEFAsync($"OrganizationUser: {request.OrganizationUserId} is not found", cancellationToken);
        
        await organizationAccessService.HasAccessOrThrow(
            request.UserId,
            organizationUser.OrganizationId,
            AccessLevel.Manage,
            cancellationToken);
        
        await coreOrganizationsService.SetUserPermissions(
            request.OrganizationUserId,
            request.UserPermissions,
            cancellationToken);
    }

    public async Task<UserPermissions> GetUserPermissions(GetUserPermissionsRequest request, CancellationToken cancellationToken)
    {
        var organizationUser = await context.OrganizationUsers
            .Where(x => x.Id == request.OrganizationUserId)
            .Select(x => new { x.OrganizationId })
            .FirstOrThrowNotFoundEFAsync($"OrganizationUser: {request.OrganizationUserId} is not found", cancellationToken);
        
        await organizationAccessService.HasAccessOrThrow(
            request.UserId,
            organizationUser.OrganizationId,
            AccessLevel.Manage,
            cancellationToken);
        
        return await coreOrganizationsService.GetUserPermissions(
            request.OrganizationUserId,
            cancellationToken);
    }

    public async Task<string> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        await organizationAccessService.HasAccessOrThrow(
            request.UserId,
            request.OrganizationId,
            AccessLevel.ReadItems,
            cancellationToken);

        return authService.CreateOrganizationToken(request.OrganizationId, request.UserId);
    }

    public async Task<OrganizationMember[]> GetOrganizationMembers(
        GetOrganizationMembersRequest request,
        CancellationToken cancellationToken)
    {
        await organizationAccessService.HasAccessOrThrow(
            request.AuthData.UserId,
            request.AuthData.OrganizationId,
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
            request.AuthData.UserId,
            request.AuthData.OrganizationId,
            AccessLevel.Manage,
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
    public required bool IsPersonal { get; set; }
}

public record JoinOrganizationRequest
{
    public Guid UserId { get; set; }
    public required string JoinCode { get; set; }
}

public record SetPermissionsRequest
{
    public Guid UserId { get; set; }
    public long OrganizationUserId { get; set; }
    public required UserPermissions UserPermissions { get; set; }
}

public record GetUserPermissionsRequest
{
    public Guid UserId { get; set; }
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