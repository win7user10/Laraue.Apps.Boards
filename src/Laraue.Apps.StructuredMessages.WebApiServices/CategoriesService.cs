using Laraue.Apps.StructuredMessages.DataAccess;
using LinqToDB.EntityFrameworkCore;

namespace Laraue.Apps.StructuredMessages.WebApiServices;

public interface ICategoriesService
{
    Task<CategoryCountDto[]> GetCategoriesWithCount(
        Guid userId,
        CancellationToken cancellationToken);
}

public class CategoriesService(DatabaseContext context) : ICategoriesService
{
    public async Task<CategoryCountDto[]> GetCategoriesWithCount(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var result = await context
            .MessageCategories
            .Where(x => x.UserId == userId)
            .Select(x => new CategoryCountDto
            {
                Id = x.Id,
                Name = x.Name,
                Count = x.Messages!.Count,
            })
            .OrderBy(x => x.Name)
            .ToListAsyncEF(cancellationToken);

        var backlogCount = await context
            .Messages
            .Where(x => x.UserId == userId)
            .Where(x => x.CategoryId == null)
            .CountAsyncEF(cancellationToken);
        
        
        return result
            .Prepend(new CategoryCountDto
            {
                Id = 0,
                Count = backlogCount,
                Name = "Backlog"
            })
            .ToArray();
    }
}

public class CategoryCountDto
{
    public required long Id { get; set; }
    public required string Name { get; set; }
    public required int Count { get; set; }
    public string Color => "#ffffff";
}