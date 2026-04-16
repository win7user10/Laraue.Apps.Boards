using Laraue.Apps.StructuredMessages.DataAccess;
using Laraue.Apps.StructuredMessages.DataAccess.Models;
using Laraue.Core.DateTime.Services.Abstractions;
using Laraue.Core.Exceptions.Web;
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace Laraue.Apps.StructuredMessages.Services;

public interface ICoreEpicsService
{
    Task<MessageCategoryListDto[]> GetList(
        Guid userId,
        CancellationToken cancellationToken);
    
    Task<long> Create(
        CreateMessageCategoryRequest request,
        CancellationToken cancellationToken);
    
    Task<bool> UserHasAccessToEpic(
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
        Action<UpdateSettersBuilder<Epic>> setters,
        CancellationToken cancellationToken);
}

public class CoreEpicsService(DatabaseContext context, IDateTimeProvider dateTimeProvider)
    : ICoreEpicsService
{
    public Task<MessageCategoryListDto[]> GetList(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return context.Epics
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
        var dateTime = dateTimeProvider.UtcNow;
        
        var category = new Epic
        {
            Name = request.Name,
            UserId = request.UserId,
            Color = request.Color ?? Palette.RandomColor(),
            CreatedAt = dateTime,
            UpdatedAt = dateTime,
            TouchedAt = dateTime,
            SpaceId = request.SpaceId,
        };
        
        var statuses = request.Statuses ?? [
            new Status
            {
                Name = CardsDefaults.DefaultStatusName,
                Color = Palette.DefaultStatusColor
            }];

        category.Statuses = statuses
            .Select((s, i) => new DataAccess.Models.Status
            {
                SortOrder = i,
                Color = s.Color,
                Name = s.Name,
            })
            .ToList();
        
        context.Epics.Add(category);
        await context.SaveChangesAsync(cancellationToken);

        return category.Id;
    }

    public Task<bool> UserHasAccessToEpic(Guid userId, long id, CancellationToken cancellationToken)
    {
        return context.Epics
            .Where(x => x.UserId == userId)
            .Where(x => x.Id == id)
            .AnyAsyncEF(cancellationToken);
    }

    public async Task ChangeStatusesOrder(
        ChangeStatusesOrderRequest request,
        CancellationToken cancellationToken)
    {
        var existsStatuses = await context.Statuses
            .Where(x => x.EpicId == request.CategoryId)
            .Select(x => new DataAccess.Models.Status
            {
                Id = x.Id,
                EpicId = x.EpicId,
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

        await context.Issues
            .Where(x => x.EpicId == request.Id)
            .ExecuteUpdateAsync(u => u
                .SetProperty(p => p.EpicId, (long?)null)
                .SetProperty(p => p.StatusId, (long?)null),
                cancellationToken);
        
        await context.Statuses
            .Where(x => x.EpicId == request.Id)
            .DeleteAsync(cancellationToken);
        
        await context.Epics
            .Where(c => c.Id == request.Id)
            .DeleteAsync(cancellationToken);
        
        await transaction.CommitAsync(cancellationToken);
    }

    public Task Update(
        long id,
        Action<UpdateSettersBuilder<Epic>> setters,
        CancellationToken cancellationToken)
    {
        var date = dateTimeProvider.UtcNow;
        
        return context.Epics
            .Where(x => x.Id == id)
            .ExecuteUpdateAsync(
                update =>
                {
                    setters(update);
                    update
                        .SetProperty(p => p.UpdatedAt, date)
                        .SetProperty(p => p.TouchedAt, date);
                },
                cancellationToken);
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
    public required long? SpaceId { get; set; }
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