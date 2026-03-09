using Laraue.Apps.StructuredMessages.DataAccess;
using Laraue.Apps.StructuredMessages.DataAccess.Models;
using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

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
    
    Task UpdateMessage(
        long messageId,
        Action<UpdateSettersBuilder<Message>> setters,
        CancellationToken cancellationToken);
    
    Task DeleteMessage(
        long id,
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

    public Task UpdateMessage(
        long messageId,
        Action<UpdateSettersBuilder<Message>> setters,
        CancellationToken cancellationToken)
    {
        return context.Messages
            .Where(x => x.Id == messageId)
            .ExecuteUpdateAsync(setters, cancellationToken);
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