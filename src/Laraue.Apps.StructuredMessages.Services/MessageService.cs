using Laraue.Apps.StructuredMessages.DataAccess;
using Laraue.Apps.StructuredMessages.DataAccess.Models;
using Microsoft.EntityFrameworkCore;

namespace Laraue.Apps.StructuredMessages.Services;

public interface IMessageService
{
    Task<long> SaveMessage(
        SaveMessageRequest request,
        CancellationToken cancellationToken);
    
    Task UpdateMessageCategory(
        UpdateMessageCategoryRequest request,
        CancellationToken cancellationToken);
}

public class MessageService(DatabaseContext context) : IMessageService
{
    public async Task<long> SaveMessage(
        SaveMessageRequest request,
        CancellationToken cancellationToken)
    {
        var entity = new Message
        {
            Content = request.Text,
            UserId = request.UserId,
            CreatedAt = request.CreatedAt,
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
            .Where(x => x.UserId == request.UserId)
            .Where(x => x.Id == request.Id)
            .ExecuteUpdateAsync(u => u
                .SetProperty(x => x.MessageTypeId, request.CategoryId),
                cancellationToken);
    }
}

public class SaveMessageRequest
{
    public Guid UserId { get; set; }
    public required string Text { get; set; }
    public required DateTime CreatedAt { get; set; }
}

public class UpdateMessageCategoryRequest
{
    public Guid UserId { get; set; }
    public required long Id { get; set; }
    public required long CategoryId { get; set; }
}