using Laraue.Apps.StructuredMessages.DataAccess;
using Laraue.Apps.StructuredMessages.DataAccess.Models;
using Laraue.Core.DataAccess.EFCore.Extensions;
using Laraue.Core.Exceptions.Web;
using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace Laraue.Apps.StructuredMessages.Services;

public interface ICoreStatusService
{
    Task<long> Create(
        CreateMessageCategoryStatusRequest request,
        CancellationToken cancellationToken);

    Task<MessageStatusDto[]> GetStatuses(
        long categoryId,
        CancellationToken cancellationToken);
    
    Task<bool> UserHasAccessToStatus(
        Guid userId,
        long id,
        CancellationToken cancellationToken);
    
    Task Delete(
        DeleteStatusRequest request,
        CancellationToken cancellationToken);
    
    Task Update(
        long id,
        Action<UpdateSettersBuilder<DataAccess.Models.Status>> setters,
        CancellationToken cancellationToken);
}

public class CoreStatusService(DatabaseContext context) : ICoreStatusService
{
    public async Task<long> Create(
        CreateMessageCategoryStatusRequest request,
        CancellationToken cancellationToken)
    {
        var previousMaxOrder = await context.Statuses
            .Where(x => x.EpicId == request.CategoryId)
            .MaxAsyncEF(x => x.SortOrder, cancellationToken);
        
        var status = new DataAccess.Models.Status
        {
            Name = request.Name,
            EpicId = request.CategoryId,
            SortOrder = ++previousMaxOrder,
            Color = request.Color ?? Palette.DefaultStatusColor,
        };
        
        context.Statuses.Add(status);
        await context.SaveChangesAsync(cancellationToken);

        return status.Id;
    }

    public Task<MessageStatusDto[]> GetStatuses(
        long categoryId,
        CancellationToken cancellationToken)
    {
        return context.Statuses
            .Where(x => x.EpicId == categoryId)
            .Select(x => new MessageStatusDto
            {
                Id = x.Id,
                Name = x.Name,
            })
            .ToArrayAsyncEF(cancellationToken);
    }

    public Task<bool> UserHasAccessToStatus(Guid userId, long id, CancellationToken cancellationToken)
    {
        return context.Statuses
            .Where(x => x.Epic!.UserId == userId)
            .Where(x => x.Id == id)
            .AnyAsyncEF(cancellationToken);
    }

    public async Task Delete(DeleteStatusRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        var categoryData = await context.Statuses
            .Where(x => x.Id == request.Id)
            .Select(x => new { MessageCategoryId = x.EpicId })
            .FirstOrThrowNotFoundEFAsync(cancellationToken);
        
        var newStatusId = await context.Statuses
            .Where(x => x.EpicId == categoryData.MessageCategoryId)
            .Where(x => x.Id != request.Id)
            .OrderBy(x => x.SortOrder)
            .Select(x => new { x.Id })
            .FirstOrDefaultAsyncEF(cancellationToken);

        if (newStatusId is null)
            throw new BadRequestException(
                nameof(request.Id),
                "Deleting the single status in category is not allowed");
        
        await context.Issues
            .Where(x => x.StatusId == request.Id)
            .ExecuteUpdateAsync(u => u
                .SetProperty(p => p.StatusId, newStatusId.Id),
                cancellationToken);
        
        await context.Statuses
            .Where(x => x.Id == request.Id)
            .ExecuteDeleteAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    public Task Update(
        long id,
        Action<UpdateSettersBuilder<DataAccess.Models.Status>> setters,
        CancellationToken cancellationToken)
    {
        return context.Statuses
            .Where(x => x.Id == id)
            .ExecuteUpdateAsync(setters, cancellationToken);
    }
}

public class CreateMessageCategoryStatusRequest
{
    public required string Name { get; set; }
    public required long CategoryId { get; set; }
    public string? Color { get; set; }
}

public class MessageStatusDto
{
    public required long Id { get; set; }
    public required string Name { get; set; }
}

public class DeleteStatusRequest
{
    public long Id { get; set; }
}