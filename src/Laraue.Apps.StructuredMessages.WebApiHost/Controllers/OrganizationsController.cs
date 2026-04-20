using Laraue.Apps.StructuredMessages.WebApiServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Laraue.Apps.StructuredMessages.WebApiHost.Controllers;

[Authorize]
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
    public Task<GetOrganizationsResponse> GetOrganizations(
        CancellationToken cancellationToken = default)
    {
        return organizationsService.GetSpaces(
            new GetOrganizationsRequest
            {
                UserId = HttpContext.User.GetId(),
            },
            cancellationToken);
    }
}