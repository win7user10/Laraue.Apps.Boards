using Laraue.Apps.StructuredMessages.WebApiServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Laraue.Apps.StructuredMessages.WebApiHost.Controllers;

[Authorize(AuthenticationSchemes = AuthSchemas.Organization)]
[ApiController]
[Route("/api/epics")]
public class EpicsController(IEpicsService categoriesService)
    : ControllerBase
{
    [HttpGet]
    public Task<EpicCountDto[]> GetAll(
        [FromQuery] GetEpicsRequest request,
        CancellationToken cancellationToken = default) => 
        categoriesService.GetEpicsWithCount(
            request with
            {
                UserId = HttpContext.User.GetId()
            },
            cancellationToken);
    
    [HttpGet("{id}")]
    public Task<CategoryDto> GetCategory(
        [FromRoute] long id,
        CancellationToken cancellationToken = default) => 
        categoriesService.GetEpic(
            new GetCategoryRequest
            {
                UserId = HttpContext.User.GetId(),
                CategoryId = id
            },
            cancellationToken);

    [HttpPost]
    public Task<long> Create(
        [FromBody] CreateEpicRequest request,
        CancellationToken cancellationToken = default)
    {
        return categoriesService.Create(
            request with
            {
                AuthData = HttpContext.User.GetOrganizationAuthData()
            },
            cancellationToken);
    }
    
    [HttpPost("{id:long}/reorder-statuses")]
    public Task ChangeStatusesOrder(
        [FromRoute] long id,
        [FromBody] IReadOnlyDictionary<long, int> order,
        CancellationToken cancellationToken = default)
    {
        return categoriesService.ChangeStatusesOrder(
            new ChangeStatusesOrderRequest
            {
                EpicId = id,
                Order = order,
                AuthData = HttpContext.User.GetOrganizationAuthData(),
            },
            cancellationToken);
    }
    
    [HttpPut("{id:long}")]
    public Task Update(
        long id,
        [FromBody] UpdateEpicRequest request,
        CancellationToken cancellationToken = default)
    {
        return categoriesService.Update(
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
        return categoriesService.Delete(
            new DeleteCategoryRequest
            {
                Id = id,
                AuthData = HttpContext.User.GetOrganizationAuthData(),
            },
            cancellationToken);
    }
}   