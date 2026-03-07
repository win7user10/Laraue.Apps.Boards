using Laraue.Apps.StructuredMessages.DataAccess;
using Laraue.Apps.StructuredMessages.DataAccess.Models;
using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Laraue.Apps.StructuredMessages.Services;

public interface IMessageService
{
    Task<bool> UserHasAccessToMessage(
        Guid userId,
        long id,
        CancellationToken cancellationToken);
    
    Task<long> SaveMessage(
        SaveMessageRequest request,
        CancellationToken cancellationToken);
    
    Task UpdateMessageCategory(
        UpdateMessageCategoryRequest request,
        CancellationToken cancellationToken);
    
    Task UpdateMessageStatus(
        UpdateMessageStatusRequest request,
        CancellationToken cancellationToken);
}

public class MessageService(DatabaseContext context) : IMessageService
{
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
        var entity = new Message
        {
            Content = request.Text,
            UserId = request.UserId,
            CreatedAt = request.CreatedAt,
            Sender = request.Sender,
            TelegramMessageId = request.TelegramMessageId,
        };
        
        context.Add(entity);
        
        await context.SaveChangesAsync(cancellationToken);
        
        return entity.Id;
    }

    public Task UpdateMessageCategory(
        UpdateMessageCategoryRequest request,
        CancellationToken cancellationToken)
    {
        return context.Messages
            .Where(x => x.Id == request.Id)
            .ExecuteUpdateAsync(u => u
                .SetProperty(x => x.CategoryId, request.CategoryId),
                cancellationToken);
    }

    public Task UpdateMessageStatus(UpdateMessageStatusRequest request, CancellationToken cancellationToken)
    {
        return context.Messages
            .Where(x => x.Id == request.Id)
            .ExecuteUpdateAsync(u => u
                .SetProperty(x => x.StatusId, request.StatusId),
                cancellationToken);
    }
}

public class SaveMessageRequest
{
    public Guid UserId { get; set; }
    public required string Text { get; set; }
    public required string? Sender { get; set; }
    public required DateTime CreatedAt { get; set; }
    public int? TelegramMessageId { get; set; }
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