using Laraue.Apps.StructuredMessages.Services;
using Laraue.Apps.StructuredMessages.WebApiServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DeleteStatusRequest = Laraue.Apps.StructuredMessages.WebApiServices.DeleteStatusRequest;

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
    
    [HttpDelete("{id:long}")]
    public Task Delete(
        long id,
        CancellationToken cancellationToken)
    {
        return statusesService.Delete(
            new DeleteStatusRequest
            {
                Id = id,
                UserId = HttpContext.User.GetId(),
            },
            cancellationToken);
    }

    [HttpPut("{id:long}")]
    public Task Edit(
        long id,
        [FromBody] EditStatusRequest request,
        CancellationToken cancellationToken)
    {
        return statusesService.Edit(
            request with
            {
                Id = id,
                UserId = HttpContext.User.GetId(),
            },
            cancellationToken);
    }

    [HttpGet]
    public Task<MessageStatusDto[]> GetStatuses(
        [FromQuery] GetStatusesRequest request,
        CancellationToken cancellationToken)
    {
        return statusesService.GetStatuses(
            request with
            {
                UserId = HttpContext.User.GetId(),
            },
            cancellationToken);
    }
}   