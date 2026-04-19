using Laraue.Apps.StructuredMessages.WebApiServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Laraue.Apps.StructuredMessages.WebApiHost.Controllers;

[Authorize]
[ApiController]
[Route("/api/epics")]
public class EpicsController(IEpicsService categoriesService)
    : ControllerBase
{
    [HttpGet]
    public Task<EpicCountResult> GetCategoriesWithCount(
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
    public Task<long> CreateCategory(
        [FromBody] CreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        return categoriesService.CreateCategory(
            request with
            {
                UserId = HttpContext.User.GetId()
            },
            cancellationToken);
    }
    
    [HttpPost("{id:long}/reorder-statuses")]
    public Task ChangeStatusesOrder(
        [FromRoute] long id,
        [FromBody] IReadOnlyDictionary<long, int> order,
        CancellationToken cancellationToken)
    {
        return categoriesService.ChangeStatusesOrder(
            new ChangeStatusesOrderRequest
            {
                CategoryId = id,
                Order = order,
                UserId = HttpContext.User.GetId(),
            },
            cancellationToken);
    }
    
    [HttpPut("{id:long}")]
    public Task Edit(
        long id,
        [FromBody] EditCategoryRequest request,
        CancellationToken cancellationToken)
    {
        return categoriesService.Edit(
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
        return categoriesService.Delete(
            new DeleteCategoryRequest
            {
                Id = id,
                UserId = HttpContext.User.GetId(),
            },
            cancellationToken);
    }
}   