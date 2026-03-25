using Laraue.Apps.StructuredMessages.DataAccess;
using Laraue.Apps.StructuredMessages.DataAccess.Models;
using Laraue.Core.Exceptions.Web;
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace Laraue.Apps.StructuredMessages.Services;

public interface ICoreCategoryService
{
    Task<MessageCategoryListDto[]> GetList(
        Guid userId,
        CancellationToken cancellationToken);
    
    Task<long> Create(
        CreateMessageCategoryRequest request,
        CancellationToken cancellationToken);
    
    Task<bool> UserHasAccessToCategory(
        Guid userId,
        long id,
        CancellationToken cancellationToken);
    
    Task ChangeStatusesOrder(
        ChangeStatusesOrderRequest request,
        CancellationToken cancellationToken);
    
    Task Delete(
        DeleteRequest request,
        CancellationToken cancellationToken);
    
    Task Update(
        long id,
        Action<UpdateSettersBuilder<CardCategory>> setters,
        CancellationToken cancellationToken);
}

public class CoreCategoryService(DatabaseContext context)
    : ICoreCategoryService
{
    public Task<MessageCategoryListDto[]> GetList(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return context.CardCategories
            .Where(x => x.UserId == userId)
            .Select(x => new MessageCategoryListDto
            {
                Name = x.Name,
                Id = x.Id
            })
            .ToArrayAsyncEF(cancellationToken);
    }

    public async Task<long> Create(
        CreateMessageCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var category = new CardCategory
        {
            Name = request.Name,
            UserId = request.UserId,
            Color = request.Color ?? Palette.RandomColor(),
        };
        
        var statuses = request.Statuses ?? [
            new Status
            {
                Name = CardsDefaults.DefaultStatusName,
                Color = Palette.DefaultStatusColor
            }];

        category.Statuses = statuses
            .Select((s, i) => new CardStatus
            {
                SortOrder = i,
                Color = s.Color,
                Name = s.Name,
            })
            .ToList();
        
        context.CardCategories.Add(category);
        await context.SaveChangesAsync(cancellationToken);

        return category.Id;
    }

    public Task<bool> UserHasAccessToCategory(Guid userId, long id, CancellationToken cancellationToken)
    {
        return context.CardCategories
            .Where(x => x.UserId == userId)
            .Where(x => x.Id == id)
            .AnyAsyncEF(cancellationToken);
    }

    public async Task ChangeStatusesOrder(
        ChangeStatusesOrderRequest request,
        CancellationToken cancellationToken)
    {
        var existsStatuses = await context.CardStatuses
            .Where(x => x.CardCategoryId == request.CategoryId)
            .Select(x => new CardStatus
            {
                Id = x.Id,
                CardCategoryId = x.CardCategoryId,
                SortOrder = x.SortOrder,
            })
            .ToArrayAsyncEF(cancellationToken);

        var existsStatusesIds = existsStatuses
            .OrderBy(x => x.Id)
            .Select(x => x.Id)
            .ToArray();
        
        if (!existsStatusesIds.SequenceEqual(request.Order.Keys))
            throw new BadRequestException(
                nameof(request.Order),
                $"The list of statuses to update does not match the statuses in category {string.Join(',', existsStatusesIds)}");

        foreach (var status in existsStatuses)
        {
            context.Attach(status);
            status.SortOrder = request.Order[status.Id];
        }
        
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task Delete(DeleteRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        await context.Cards
            .Where(x => x.CategoryId == request.Id)
            .ExecuteUpdateAsync(u => u
                .SetProperty(p => p.CategoryId, (long?)null)
                .SetProperty(p => p.StatusId, (long?)null),
                cancellationToken);
        
        await context.CardStatuses
            .Where(x => x.CardCategoryId == request.Id)
            .DeleteAsync(cancellationToken);
        
        await context.CardCategories
            .Where(c => c.Id == request.Id)
            .DeleteAsync(cancellationToken);
        
        await transaction.CommitAsync(cancellationToken);
    }

    public Task Update(
        long id,
        Action<UpdateSettersBuilder<CardCategory>> setters,
        CancellationToken cancellationToken)
    {
        return context.CardCategories
            .Where(x => x.Id == id)
            .ExecuteUpdateAsync(setters, cancellationToken);
    }
}

public class MessageCategoryListDto
{
    public long Id { get; set; }
    public required string Name { get; set; }
}

public class ChangeStatusesOrderRequest
{
    public required long CategoryId { get; set; }
    
    /// <summary>
    /// Order map, status id -> order value.
    /// </summary>
    public required IReadOnlyDictionary<long, int> Order { get; set; }
}

public class CreateMessageCategoryRequest
{
    public required string Name { get; set; }
    public string? Color { get; set; }
    public required Guid UserId { get; set; }
    public Status[]? Statuses { get; set; }
}

public class Status
{
    public required string Name { get; set; }
    public required string Color { get; set; }
}

public record DeleteRequest
{
    public required long Id { get; set; }
}