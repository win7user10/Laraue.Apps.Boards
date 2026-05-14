using Laraue.Apps.StructuredMessages.Services;
using Laraue.Apps.StructuredMessages.WebApiServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Laraue.Apps.StructuredMessages.WebApiHost.Controllers;

[Authorize(AuthenticationSchemes = AuthSchemas.User)]
[ApiController]
[Route("/api/organizations")]
public class OrganizationsController(IOrganizationsService organizationsService) : ControllerBase
{
    [HttpPost]
    public Task<long> Create(
        [FromBody] CreateOrganizationRequest request,
        CancellationToken cancellationToken = default)
    {
        return organizationsService.Create(
            request with
            {
                UserId = HttpContext.User.GetId()
            },
            cancellationToken);
    }
    
    [HttpPut("{id:long}")]
    public Task Update(
        long id,
        [FromBody] EditOrganizationRequest request,
        CancellationToken cancellationToken = default)
    {
        return organizationsService.Update(
            request with
            {
                Id = id,
                UserId = HttpContext.User.GetId(),
            },
            cancellationToken);
    }
    
    [HttpDelete("{id:long}")]
    public Task Delete(
        long id,
        CancellationToken cancellationToken = default)
    {
        return organizationsService.Delete(
            new DeleteOrganizationRequest
            {
                Id = id,
                UserId = HttpContext.User.GetId(),
            },
            cancellationToken);
    }
    
    [HttpGet]
    public Task<OrganizationDto[]> GetOrganizations(
        CancellationToken cancellationToken = default)
    {
        return organizationsService.GetOrganizations(
            new GetOrganizationsRequest
            {
                UserId = HttpContext.User.GetId(),
            },
            cancellationToken);
    }
    
    [HttpPost("join/{code}")]
    public Task Join(
        string code,
        CancellationToken cancellationToken = default)
    {
        return organizationsService.Join(
            new JoinOrganizationRequest
            {
                JoinCode = code,
                UserId = HttpContext.User.GetId(),
            },
            cancellationToken);
    }
    
    [HttpPost("{id:long}/leave")]
    public Task Leave(
        long id,
        CancellationToken cancellationToken = default)
    {
        return organizationsService.Leave(
            new LeaveOrganizationRequest
            {
                UserId = HttpContext.User.GetId(),
                OrganizationId = id,
            },
            cancellationToken);
    }
    
    [Authorize(AuthenticationSchemes = AuthSchemas.Organization)]
    [HttpPost("regenerate-join-code")]
    public Task<string> RegenerateCode(
        CancellationToken cancellationToken = default)
    {
        return organizationsService.RegenerateJoinCode(
            new RegenerateJoinCodeRequest
            {
                AuthData = HttpContext.User.GetOrganizationAuthData(),
            },
            cancellationToken);
    }
    
    [Authorize(AuthenticationSchemes = AuthSchemas.Organization)]
    [HttpGet("join-code")]
    public Task<string?> GetJoinCode(
        CancellationToken cancellationToken = default)
    {
        return organizationsService.GetOrganizationJoinCode(
            new GetOrganizationJoinCodeRequest
            {
                AuthData = HttpContext.User.GetOrganizationAuthData(),
            },
            cancellationToken);
    }
    
    [Authorize(AuthenticationSchemes = AuthSchemas.Organization)]
    [HttpPost("revoke-access/{organizationUserId:long}")]
    public Task RevokeAccess(
        long organizationUserId,
        CancellationToken cancellationToken = default)
    {
        return organizationsService.RevokeAccess(
            new RevokeAccessRequest
            {
                OrganizationUserId = organizationUserId,
                AuthData = HttpContext.User.GetOrganizationAuthData(),
            },
            cancellationToken);
    }
    
    [HttpPost("login")]
    public Task<string> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        return organizationsService.Login(
            request with
            {
                UserId = HttpContext.User.GetId(),
            },
            cancellationToken);
    }
    
    [Authorize(AuthenticationSchemes = AuthSchemas.Organization)]
    [HttpGet("permissions/{organizationUserId:long}")]
    public Task<UserPermissions> GetUserPermissions(
        long organizationUserId,
        CancellationToken cancellationToken = default)
    {
        return organizationsService.GetUserPermissions(
            new GetUserPermissionsRequest
            {
                OrganizationUserId = organizationUserId,
                AuthData = HttpContext.User.GetOrganizationAuthData(),
            },
            cancellationToken);
    }
    
    [Authorize(AuthenticationSchemes = AuthSchemas.Organization)]
    [HttpPost("permissions/{organizationUserId:long}")]
    public Task SetUserPermissions(
        long organizationUserId,
        [FromBody] SetPermissionsRequest request,
        CancellationToken cancellationToken = default)
    {
        return organizationsService.SetUserPermissions(
            request with
            {
                AuthData = HttpContext.User.GetOrganizationAuthData(),
                OrganizationUserId = organizationUserId
            },
            cancellationToken);
    }
    
    [Authorize(AuthenticationSchemes = AuthSchemas.Organization)]
    [HttpGet("permittable-entities")]
    public Task<PermittableSpace[]> GetPermittableEntities(
        CancellationToken cancellationToken = default)
    {
        return organizationsService.GetPermittableEntities(
            new GetPermittableEntitiesRequest
            {
                AuthData = HttpContext.User.GetOrganizationAuthData(),
            },
            cancellationToken);
    }
    
    [Authorize(AuthenticationSchemes = AuthSchemas.Organization)]
    [HttpGet("current")]
    public Task<OrganizationDto> GetOrganization(
        CancellationToken cancellationToken = default)
    {
        return organizationsService.GetOrganization(
            new GetOrganizationRequest
            {
                AuthData = HttpContext.User.GetOrganizationAuthData()
            },
            cancellationToken);
    }
    
    [Authorize(AuthenticationSchemes = AuthSchemas.Organization)]
    [HttpGet("members")]
    public Task<OrganizationMember[]> GetOrganizationMembers(
        CancellationToken cancellationToken = default)
    {
        return organizationsService.GetOrganizationMembers(
            new GetOrganizationMembersRequest
            {
                AuthData = HttpContext.User.GetOrganizationAuthData()
            },
            cancellationToken);
    }
}