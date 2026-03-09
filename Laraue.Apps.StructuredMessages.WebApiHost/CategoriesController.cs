using Laraue.Apps.StructuredMessages.WebApiServices;
using Microsoft.AspNetCore.Mvc;

namespace Laraue.Apps.StructuredMessages.WebApiHost;

[ApiController]
[Route("/api/categories")]
public class CategoriesController(ICategoriesService categoriesService) : ControllerBase
{
    [HttpGet("categories-with-count")]
    public Task<CategoryCountDto[]> GetCategoriesWithCount(CancellationToken cancellationToken)
        => categoriesService.GetCategoriesWithCount(
            new Guid("019cc73f-4bb9-79c0-bb7e-f35d6d911df4"),
            cancellationToken);
}   