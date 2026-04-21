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
    Task<GetOrganizationsResponse> GetOrganizations(
        GetOrganizationsRequest request,
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
    
    Task SetPermissions(
        SetPermissionsRequest request,
        CancellationToken cancellationToken);
    
    Task<Permissions> GetPermissions(
        GetPermissionsRequest request,
        CancellationToken cancellationToken);
    
    Task<string> Login(
        LoginRequest request,
        CancellationToken cancellationToken);
}

public class OrganizationsService(
    ICoreOrganizationsService coreOrganizationsService,
    DatabaseContext context,
    IAuthService authService)
    : IOrganizationsService
{
    public async Task<GetOrganizationsResponse> GetOrganizations(
        GetOrganizationsRequest request,
        CancellationToken cancellationToken)
    {
        var allOrganizations = await AvailableOrganizations(request.UserId)
            .OrderBy(x => x.Name)
            .Select(x => new OrganizationDto
            {
                Id = x.Id,
                AccessLevel = x.Users!.FirstOrDefault(u => u.UserId == request.UserId) != null
                    ? x.Users!.FirstOrDefault(u => u.UserId == request.UserId)!.AccessLevel
                    : AccessLevel.Manage,
                Name = x.Name,
                Color = x.Color,
                SpacesCount = x.Spaces!.Count
            })
            .ToArrayAsyncEF(cancellationToken);

        var noSpacesCount = await context.Spaces
            .Where(x => x.CreatorId == request.UserId)
            .Where(x => x.OrganizationId == null)
            .CountAsyncEF(cancellationToken);

        return new GetOrganizationsResponse
        {
            Organizations = allOrganizations,
            PersonalOrganizationSpacesCount = noSpacesCount
        };
    }

    private IQueryable<Organization> AvailableOrganizations(Guid userId)
    {
        var userOrganizationsQuery = context.OrganizationUsers
            .Where(x => x.UserId == userId)
            .Select(x => x.Organization!);

        var userOwnedOrganizationsQuery = context.Organizations
            .Where(x => x.OwnerId == userId)
            .Select(x => x);
        
        return userOrganizationsQuery
            .Union(userOwnedOrganizationsQuery);
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
        if (!await coreOrganizationsService.HasAccess(
            request.Id,
            request.UserId,
            AccessLevel.Update,
            cancellationToken))
            throw new NotFoundException();

        await coreOrganizationsService.Update(
            request.Id,
            setters => setters
                .SetProperty(x => x.Color, request.Color)
                .SetProperty(x => x.Name, request.Name),
            cancellationToken);
    }

    public async Task Delete(DeleteOrganizationRequest request, CancellationToken cancellationToken)
    {
        if (!await coreOrganizationsService.HasAccess(
            request.Id,
            request.UserId,
            AccessLevel.Delete,
            cancellationToken))
            throw new NotFoundException();
        
        await coreOrganizationsService.Delete(request.Id, cancellationToken);
    }

    public async Task Join(JoinOrganizationRequest request, CancellationToken cancellationToken)
    {
        var organizationId = await coreOrganizationsService.GetOrganizationIdByJoinCode(
            request.JoinCode,
            cancellationToken);
        
        if (organizationId == null)
            throw new NotFoundException();

        await coreOrganizationsService.AddMember(
            organizationId.Value,
            request.UserId,
            cancellationToken);
    }

    public async Task SetPermissions(SetPermissionsRequest request, CancellationToken cancellationToken)
    {
        // TODO - check that passed permissions are correct, check access to passed items
        var organizationUser = await context.OrganizationUsers
            .Where(x => x.Id == request.OrganizationUserId)
            .Select(x => new { x.OrganizationId })
            .FirstOrThrowNotFoundEFAsync(cancellationToken);
        
        if (!await coreOrganizationsService.HasAccess(
            organizationUser.OrganizationId,
            request.UserId,
            AccessLevel.Manage,
            cancellationToken))
            throw new NotFoundException();
        
        await coreOrganizationsService.SetPermissions(
            request.OrganizationUserId,
            request.Permissions,
            cancellationToken);
    }

    public async Task<Permissions> GetPermissions(GetPermissionsRequest request, CancellationToken cancellationToken)
    {
        var organizationUser = await context.OrganizationUsers
            .Where(x => x.Id == request.OrganizationUserId)
            .Select(x => new { x.OrganizationId })
            .FirstOrThrowNotFoundEFAsync(cancellationToken);
        
        if (!await coreOrganizationsService.HasAccess(
            organizationUser.OrganizationId,
            request.UserId,
            AccessLevel.Manage,
            cancellationToken))
            throw new NotFoundException();
        
        return await coreOrganizationsService.GetPermissions(
            request.OrganizationUserId,
            cancellationToken);
    }

    public async Task<string> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        await AvailableOrganizations(request.UserId)
            .AnyOrThrowNotFoundEFAsync(x => x.Id == request.OrganizationId, cancellationToken);

        return authService.CreateOrganizationToken(request.OrganizationId, request.UserId);
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

public record OrganizationDto
{
    public long Id { get; set; }
    public required string Name { get; set; }
    public required string? Color { get; set; }
    public required int SpacesCount { get; set; }
    public required AccessLevel AccessLevel { get; set; }
}

public record GetOrganizationsResponse
{
    public required OrganizationDto[] Organizations { get; set; }
    public required int PersonalOrganizationSpacesCount { get; set; }
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
    public required Permissions Permissions { get; set; }
}

public record GetPermissionsRequest
{
    public Guid UserId { get; set; }
    public long OrganizationUserId { get; set; }
}

public record LoginRequest
{
    public Guid UserId { get; set; }
    public long OrganizationId { get; set; }
}