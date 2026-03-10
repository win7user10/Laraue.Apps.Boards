using System.ComponentModel.DataAnnotations;
using Laraue.Apps.StructuredMessages.DataAccess;
using Laraue.Apps.StructuredMessages.DataAccess.Models;
using LinqToDB.EntityFrameworkCore;

namespace Laraue.Apps.StructuredMessages.WebApiServices;

public interface ICategoriesService
{
    Task<CategoryCountDto[]> GetCategoriesWithCount(
        Guid userId,
        CancellationToken cancellationToken);
    
    Task<CategoryDto> GetCategory(
        GetCategoryRequest request,
        CancellationToken cancellationToken);
    
    Task<long> CreateCategory(
        CreateCategoryRequest request,
        CancellationToken cancellationToken);
}

public class CategoriesService(DatabaseContext context)
    : ICategoriesService
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
                Color = x.Color,
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
                Name = "Backlog",
                Color = "#000000",
            })
            .ToArray();
    }

    public Task<CategoryDto> GetCategory(GetCategoryRequest request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task<long> CreateCategory(
        CreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var category = new MessageCategory
        {
            Name = request.Name,
            UserId = request.UserId,
            Color = request.Color,
        };
        
        context.MessageCategories.Add(category);
        await context.SaveChangesAsync(cancellationToken);

        return category.Id;
    }
}

public record CategoryCountDto
{
    public required long Id { get; set; }
    public required string Name { get; set; }
    public required int Count { get; set; }
    public required string? Color { get; set; }
}

public record GetCategoryRequest
{
    public required Guid UserId { get; set; }
    public required long CategoryId { get; set; }
}

public record CategoryDto
{
    
}

public record CreateCategoryRequest
{
    public Guid UserId { get; set; }
    
    [MaxLength(128)]
    public required string Name { get; set; }
    
    [MaxLength(7)]
    public required string Color { get; set; }
}