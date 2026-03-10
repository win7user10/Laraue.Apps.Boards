using Laraue.Apps.StructuredMessages.WebApiServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Laraue.Apps.StructuredMessages.WebApiHost.Controllers;

[Authorize]
[ApiController]
[Route("/api/categories")]
public class CategoriesController(ICategoriesService categoriesService)
    : ControllerBase
{
    [HttpGet("categories-with-count")]
    public Task<CategoryCountDto[]> GetCategoriesWithCount(CancellationToken cancellationToken) => 
        categoriesService.GetCategoriesWithCount(
            HttpContext.User.GetId(),
            cancellationToken);
    
    
    [HttpGet("{id}")]
    public Task<CategoryDto> GetCategory(
        [FromRoute] long id,
        CancellationToken cancellationToken) => 
        categoriesService.GetCategory(
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
}   