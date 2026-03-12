using System.ComponentModel.DataAnnotations;
using Laraue.Apps.StructuredMessages.DataAccess;
using Laraue.Apps.StructuredMessages.DataAccess.Models;
using Laraue.Apps.StructuredMessages.Services;
using Laraue.Core.DataAccess.EFCore.Extensions;
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
                StatusesCount = x.Statuses!.Count,
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
                Id = CoreMessageService.NullId,
                Count = backlogCount,
                Name = "Backlog",
                Color = "#000000",
                StatusesCount = 0,
            })
            .ToArray();
    }

    public Task<CategoryDto> GetCategory(
        GetCategoryRequest request,
        CancellationToken cancellationToken)
    {
        return context
            .MessageCategories
            .Where(x => x.Id == request.CategoryId)
            .Select(x => new CategoryDto
            {
                Color = x.Color,
                Name = x.Name,
                Statuses = x.Statuses!
                    .Select(s => new StatusDto
                    {
                        Id = s.Id,
                        Color = s.Color,
                        Name = s.Name,
                    })
                    .ToArray(),
            })
            .FirstOrThrowNotFoundEFAsync(cancellationToken);
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
    public required int StatusesCount { get; set; }
}

public record GetCategoryRequest
{
    public required Guid UserId { get; set; }
    public required long CategoryId { get; set; }
}

public record CategoryDto
{
    public required string Name { get; set; }
    public required string? Color { get; set; }
    public StatusDto[] Statuses { get; set; } = [];
}

public class StatusDto
{
    public required long Id { get; set; }
    public required string Name { get; set; }
    public required string? Color { get; set; }
}

public record CreateCategoryRequest
{
    public Guid UserId { get; set; }
    
    [MaxLength(128)]
    public required string Name { get; set; }
    
    [MaxLength(7)]
    public required string Color { get; set; }
}