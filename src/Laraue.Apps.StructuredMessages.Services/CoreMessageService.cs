using Laraue.Apps.StructuredMessages.DataAccess;
using Laraue.Apps.StructuredMessages.DataAccess.Models;
using Laraue.Core.DataAccess.EFCore.Extensions;
using Laraue.Core.Exceptions.Web;
using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace Laraue.Apps.StructuredMessages.Services;

public interface ICoreMessageService
{
    Task<bool> UserHasAccessToMessage(
        Guid userId,
        long id,
        CancellationToken cancellationToken);
    
    Task<long> SaveMessage(
        SaveMessageRequest request,
        CancellationToken cancellationToken);
    
    Task UpdateMessage(
        long messageId,
        Action<UpdateSettersBuilder<Message>> setters,
        CancellationToken cancellationToken);
    
    Task UpdateStatus(
        long messageId,
        long newStatusId,
        CancellationToken ct);
    
    Task UpdateCategory(
        long messageId,
        long newCategoryId,
        CancellationToken ct);
    
    Task DeleteMessage(
        long id,
        CancellationToken cancellationToken);
}

public class CoreMessageService(DatabaseContext context) : ICoreMessageService
{
    public const long NullId = 0;
    
    public Task<bool> UserHasAccessToMessage(Guid userId, long id, CancellationToken cancellationToken)
    {
        return context.Messages
            .Where(x => x.UserId == userId)
            .Where(x => x.Id == id)
            .AnyAsyncEF(cancellationToken);
    }

    public async Task<long> SaveMessage(
        SaveMessageRequest request,
        CancellationToken cancellationToken)
    {
        if (request.CategoryId is null && request.StatusId is not null)
            throw new BadRequestException(
                nameof(request.StatusId),
                "To set the status need to specify category");

        if (request.StatusId is not null)
        {
            var statusAvailable = await context.MessageStatuses
                .Where(x => x.MessageCategoryId == request.CategoryId)
                .Where(x => x.Id == request.StatusId)
                .AnyAsyncEF(cancellationToken);
            
            if (!statusAvailable)
                throw new BadRequestException(
                    nameof(request.StatusId),
                    "Status is not found in the category");
        }
        
        var entity = new Message
        {
            Content = request.Text,
            UserId = request.UserId,
            CreatedAt = request.CreatedAt,
            Sender = request.Sender,
            TelegramMessageId = request.TelegramMessageId,
            CategoryId = request.CategoryId,
            StatusId = request.StatusId,
        };
        
        context.Add(entity);
        
        await context.SaveChangesAsync(cancellationToken);
        
        return entity.Id;
    }

    public Task UpdateMessage(
        long messageId,
        Action<UpdateSettersBuilder<Message>> setters,
        CancellationToken cancellationToken)
    {
        return context.Messages
            .Where(x => x.Id == messageId)
            .ExecuteUpdateAsync(setters, cancellationToken);
    }

    public async Task UpdateStatus(long messageId, long newStatusId, CancellationToken ct)
    {
        if (newStatusId != NullId)
        {
            var possibleStatusesIds = await context.Messages
                .Where(x => x.Id == messageId)
                .Select(x => x.Category!.Statuses!
                    .Select(s => s.Id))
                .FirstOrThrowNotFoundEFAsync(ct);

            if (!possibleStatusesIds.Contains(newStatusId))
                throw new BadRequestException(
                    nameof(newStatusId),
                    "Incorrect Status Id");
        }

        long? value = newStatusId == NullId ? null : newStatusId;

        await context.Messages
            .Where(x => x.Id == messageId)
            .ExecuteUpdateAsync(update => update
                .SetProperty(x => x.StatusId, value),
                ct);
    }

    public async Task UpdateCategory(long messageId, long newCategoryId, CancellationToken ct)
    {
        if (newCategoryId != NullId)
        {
            var userId = await context.Messages
                .Where(x => x.Id == messageId)
                .Select(x => x.UserId)
                .FirstOrDefaultAsyncEF(ct);
        
            var possibleCategoryIds = await context.MessageCategories
                .Where(x => x.UserId == userId)
                .Select(x => x.Id)
                .ToArrayAsyncEF(ct);
        
            if (!possibleCategoryIds.Contains(newCategoryId))
                throw new BadRequestException(
                    nameof(newCategoryId),
                    "Incorrect Category Id");
        }
        
        long? value = newCategoryId == NullId ? null : newCategoryId;
        
        // Set default category status after moving to category
        long? newStatus = NullId;
        if (value is not null)
        {
            var newStatusData = await context.MessageStatuses
                .Where(s => s.MessageCategoryId == value.Value)
                .OrderBy(s => s.SortOrder)
                .Select(s => new { s.Id })
                .FirstOrDefaultAsyncEF(ct);
            
            newStatus = newStatusData?.Id;
        }
        
        await context.Messages
            .Where(x => x.Id == messageId)
            .ExecuteUpdateAsync(update => update
                .SetProperty(x => x.CategoryId, value)
                .SetProperty(x => x.StatusId, newStatus),
                ct);
    }

    public Task DeleteMessage(long id, CancellationToken cancellationToken)
    {
        return context.Messages
            .Where(x => x.Id == id)
            .ExecuteDeleteAsync(cancellationToken);
    }
}

public class SaveMessageRequest
{
    public Guid UserId { get; set; }
    public required string Text { get; set; }
    public required string? Sender { get; set; }
    public required DateTime CreatedAt { get; set; }
    public int? TelegramMessageId { get; set; }
    public long? CategoryId { get; set; }
    public long? StatusId { get; set; }
}

public class UpdateMessageCategoryRequest
{
    public required long Id { get; set; }
    public required long CategoryId { get; set; }
}

public class UpdateMessageStatusRequest
{
    public required long Id { get; set; }
    public required long StatusId { get; set; }
}

public class UpdateMessageTextRequest
{
    public required long Id { get; set; }
    public required string Content { get; set; }
}