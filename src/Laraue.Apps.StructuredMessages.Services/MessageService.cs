using Laraue.Apps.StructuredMessages.DataAccess;
using Laraue.Apps.StructuredMessages.DataAccess.Models;

namespace Laraue.Apps.StructuredMessages.Services;

public interface IMessageService
{
    Task<long> SaveMessage(
        SaveMessageRequest request,
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
}

public class SaveMessageRequest
{
    public Guid UserId { get; set; }
    public required string Text { get; set; }
    public required DateTime CreatedAt { get; set; }
}