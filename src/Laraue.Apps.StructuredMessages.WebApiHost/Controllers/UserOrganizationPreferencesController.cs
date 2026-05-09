using Laraue.Apps.StructuredMessages.Services;
using Laraue.Apps.StructuredMessages.WebApiServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Laraue.Apps.StructuredMessages.WebApiHost.Controllers;

[Authorize(AuthenticationSchemes = AuthSchemas.Organization)]
[ApiController]
[Route("/api/user-organization-preferences")]
public class UserOrganizationPreferencesController(IUserOrganizationPreferencesService service) : ControllerBase
{
    [HttpGet]
    public Task<UserOrganizationPreferencesResponse> GetSettings(
        CancellationToken cancellationToken)
    {
        return service.GetPreferences(
            HttpContext.User.GetOrganizationAuthData(),
            cancellationToken);
    }

    
    [HttpPut("space/{spaceId}")]
    public Task UpdateSpaceId(
        [FromRoute] long spaceId,
        CancellationToken cancellationToken)
    {
        return service.UpdateSelectedSpace(
            HttpContext.User.GetOrganizationAuthData(),
            spaceId, cancellationToken);
    }
}