using Laraue.Apps.StructuredMessages.DataAccess.Models;
using Laraue.Apps.StructuredMessages.Services;
using Laraue.Apps.StructuredMessages.WebApiServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Laraue.Apps.StructuredMessages.WebApiHost.Controllers;

[Authorize(AuthenticationSchemes = AuthSchemas.Organization)]
[ApiController]
[Route("/api/user-preferences")]
public class UserPreferencesController(IUserPreferencesService service) : ControllerBase
{
    [HttpGet]
    public Task<UserPreferencesResponse> GetSettings(
        CancellationToken cancellationToken)
    {
        return service.GetPreferences(HttpContext.User.GetId(), cancellationToken);
    }
    
    [HttpPut("epic-sort-order/{epicSortOrder}")]
    public Task UpdateEpicSortOrder(
        [FromRoute] EpicSortOrder epicSortOrder,
        CancellationToken cancellationToken)
    {
        return service.UpdateEpicSortOrder(HttpContext.User.GetId(), epicSortOrder, cancellationToken);
    }
    
    [HttpPut("space/{spaceId}")]
    public Task UpdateSpaceId(
        [FromRoute] long spaceId,
        CancellationToken cancellationToken)
    {
        return service.UpdateSpace(HttpContext.User.GetId(), spaceId, cancellationToken);
    }
}