using Laraue.Apps.StructuredMessages.WebApiServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Laraue.Apps.StructuredMessages.WebApiHost.Controllers;

[Authorize]
[ApiController]
[Route("/api/statuses")]
public class StatusesController(IStatusesService statusesService)
    : ControllerBase
{
    [HttpPost]
    public Task<long> CreateStatus(
        [FromBody] CreateStatusRequest request,
        CancellationToken cancellationToken)
    {
        return statusesService.CreateStatus(
            request with
            {
                UserId = HttpContext.User.GetId()
            },
            cancellationToken);
    }
}   