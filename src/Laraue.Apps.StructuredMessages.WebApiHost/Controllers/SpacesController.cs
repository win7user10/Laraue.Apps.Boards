using Laraue.Apps.StructuredMessages.WebApiServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Laraue.Apps.StructuredMessages.WebApiHost.Controllers;

[Authorize(AuthenticationSchemes = AuthSchemas.Organization)]
[ApiController]
[Route("/api/spaces")]
public class SpacesController(ISpacesService spacesService, IEpicsService epicsService) : ControllerBase
{
    [HttpPost]
    public Task<long> Create(
        [FromBody] CreateSpaceRequest request,
        CancellationToken cancellationToken = default)
    {
        return spacesService.Create(
            request with
            {
                AuthData = HttpContext.User.GetOrganizationAuthData()
            },
            cancellationToken);
    }
    
    [HttpPut("{id:long}")]
    public Task Update(
        long id,
        [FromBody] UpdateSpaceRequest request,
        CancellationToken cancellationToken = default)
    {
        return spacesService.Update(
            request with
            {
                Id = id,
                AuthData = HttpContext.User.GetOrganizationAuthData(),
            },
            cancellationToken);
    }
    
    [HttpDelete("{id:long}")]
    public Task Delete(
        long id,
        CancellationToken cancellationToken = default)
    {
        return spacesService.Delete(
            new DeleteSpaceRequest
            {
                Id = id,
                AuthData = HttpContext.User.GetOrganizationAuthData(),
            },
            cancellationToken);
    }
    
    [HttpGet]
    public Task<SpaceListDto[]> GetAll(
        CancellationToken cancellationToken = default)
    {
        return spacesService.GetSpaces(
            new GetSpacesRequest
            {
                AuthData = HttpContext.User.GetOrganizationAuthData(),
            },
            cancellationToken);
    }
    
    [HttpGet("{id:long}/epics")]
    public Task<EpicListDto[]> GetSpaceEpics(
        long id,
        CancellationToken cancellationToken = default) => 
        epicsService.GetSpaceEpics(
            new GetEpicsRequest
            {
                SpaceId = id,
                AuthData = HttpContext.User.GetOrganizationAuthData()
            },
            cancellationToken);
}