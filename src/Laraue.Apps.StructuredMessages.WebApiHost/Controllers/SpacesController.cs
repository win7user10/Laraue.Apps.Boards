using Laraue.Apps.StructuredMessages.WebApiServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Laraue.Apps.StructuredMessages.WebApiHost.Controllers;

[Authorize]
[ApiController]
[Route("/api/spaces")]
public class SpacesController(ISpacesService spacesService) : ControllerBase
{
    [HttpPost]
    public Task<long> Create(
        [FromBody] CreateSpaceRequest request,
        CancellationToken cancellationToken)
    {
        return spacesService.Create(
            request with
            {
                UserId = HttpContext.User.GetId()
            },
            cancellationToken);
    }
    
    [HttpPut("{id:long}")]
    public Task Update(
        long id,
        [FromBody] EditSpaceRequest request,
        CancellationToken cancellationToken)
    {
        return spacesService.Update(
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
        CancellationToken cancellationToken)
    {
        return spacesService.Delete(
            new DeleteSpaceRequest
            {
                Id = id,
                UserId = HttpContext.User.GetId(),
            },
            cancellationToken);
    }
    
    [HttpGet]
    public Task<SpaceDto[]> Get(
        CancellationToken cancellationToken)
    {
        return spacesService.GetSpaces(
            new GetSpacesRequest
            {
                UserId = HttpContext.User.GetId(),
            },
            cancellationToken);
    }
}