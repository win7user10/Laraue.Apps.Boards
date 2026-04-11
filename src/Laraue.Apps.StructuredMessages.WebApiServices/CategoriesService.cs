using System.ComponentModel.DataAnnotations;
using Laraue.Apps.StructuredMessages.DataAccess;
using Laraue.Apps.StructuredMessages.Services;
using Laraue.Core.DataAccess.EFCore.Extensions;
using Laraue.Core.Exceptions.Web;
using LinqToDB.EntityFrameworkCore;

namespace Laraue.Apps.StructuredMessages.WebApiServices;

public interface ICategoriesService
{
    Task<CategoryCountResult> GetCategoriesWithCount(
        Guid userId,
        CancellationToken cancellationToken);
    
    Task<CategoryDto> GetCategory(
        GetCategoryRequest request,
        CancellationToken cancellationToken);
    
    Task<long> CreateCategory(
        CreateCategoryRequest request,
        CancellationToken cancellationToken);
    
    Task ChangeStatusesOrder(
        ChangeStatusesOrderRequest request,
        CancellationToken cancellationToken);
    
    Task Edit(
        EditCategoryRequest request,
        CancellationToken cancellationToken);
    
    Task Delete(
        DeleteCategoryRequest request,
        CancellationToken cancellationToken);
}

public class CategoriesService(
    DatabaseContext context,
    ICoreCategoryService coreCategoryService)
    : ICategoriesService
{
    public async Task<CategoryCountResult> GetCategoriesWithCount(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var result = await context
            .Epics
            .Where(x => x.UserId == userId)
            .Select(x => new CategoryCountDto
            {
                Id = x.Id,
                Name = x.Name,
                Count = x.Issues!.Count,
                Color = x.Color,
                StatusesCount = x.Statuses!.Count,
            })
            .OrderBy(x => x.Name)
            .ToArrayAsyncEF(cancellationToken);

        var backlogCount = await context
            .Issues
            .Where(x => x.UserId == userId)
            .Where(x => x.EpicId == null)
            .CountAsyncEF(cancellationToken);

        return new CategoryCountResult
        {
            Categories = result,
            BacklogCount = backlogCount
        };
    }

    public Task<CategoryDto> GetCategory(
        GetCategoryRequest request,
        CancellationToken cancellationToken)
    {
        return context
            .Epics
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
                        SortOrder = s.SortOrder,
                    })
                    .ToArray(),
            })
            .FirstOrThrowNotFoundEFAsync(cancellationToken);
    }

    public Task<long> CreateCategory(
        CreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        return coreCategoryService.Create(
            new CreateMessageCategoryRequest
            {
                UserId = request.UserId,
                Name = request.Name,
                Color = request.Color,
            },
            cancellationToken);
    }

    public async Task ChangeStatusesOrder(
        ChangeStatusesOrderRequest request,
        CancellationToken cancellationToken)
    {
        if (!await coreCategoryService
            .UserHasAccessToCategory(request.UserId, request.CategoryId, cancellationToken))
            throw new NotFoundException();
        
        await coreCategoryService.ChangeStatusesOrder(
            new Services.ChangeStatusesOrderRequest
            {
                CategoryId = request.CategoryId,
                Order = request.Order
            },
            cancellationToken);
    }

    public async Task Edit(EditCategoryRequest request, CancellationToken cancellationToken)
    {
        if (!await coreCategoryService
            .UserHasAccessToCategory(request.UserId, request.Id, cancellationToken))
            throw new NotFoundException();

        await coreCategoryService.Update(
            request.Id,
            upd => upd
                .SetProperty(x => x.Color, request.Color)
                .SetProperty(x => x.Name, request.Name),
            cancellationToken);
    }

    public async Task Delete(DeleteCategoryRequest request, CancellationToken cancellationToken)
    {
        if (!await coreCategoryService
                .UserHasAccessToCategory(request.UserId, request.Id, cancellationToken))
            throw new NotFoundException();
        
        await coreCategoryService.Delete(
            new DeleteRequest { Id = request.Id },
            cancellationToken);
    }
}

public class CategoryCountResult
{
    public int BacklogCount { get; set; }
    public required CategoryCountDto[] Categories { get; set; }
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
    public required int SortOrder { get; set; }
}

public record CreateCategoryRequest
{
    public Guid UserId { get; set; }
    
    [MaxLength(128)]
    public required string Name { get; set; }
    
    [MaxLength(7)]
    public required string Color { get; set; }
}

public record ChangeStatusesOrderRequest
{
    public Guid UserId { get; set; }
    public required long CategoryId { get; set; }
    public required IReadOnlyDictionary<long, int> Order { get; set; }
}

public record EditCategoryRequest
{
    public Guid UserId { get; set; }
    
    public long Id { get; set; }
    
    [MaxLength(128)]
    public required string Name { get; set; }
    
    [MaxLength(7)]
    public required string Color { get; set; }
}

public record DeleteCategoryRequest
{
    public Guid UserId { get; set; }
    
    public long Id { get; set; }
}