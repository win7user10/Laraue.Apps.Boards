using Laraue.Apps.StructuredMessages.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Laraue.Apps.StructuredMessages.WebApiServices;

public interface IMessagesService
{
    Task<MessageListDto[]> GetBacklogMessages(
        GetBacklogMessagesRequest request,
        CancellationToken cancellationToken);
    
    Task<MessageListDto[]> GetMessages(
        GetMessagesRequest request,
        CancellationToken cancellationToken);
}

public class MessagesService(DatabaseContext context) : IMessagesService
{
    public Task<MessageListDto[]> GetBacklogMessages(
        GetBacklogMessagesRequest request,
        CancellationToken cancellationToken)
    {
        return context
            .Messages
            .Where(x => x.UserId == request.UserId)
            .Where(x => x.CategoryId == null)
            .OrderByDescending(x => x.Id)
            .Select(x => new MessageListDto
            {
                Id = x.Id,
                Sender = x.Sender,
                Text = x.Content,
                Time = x.CreatedAt
            })
            .ToArrayAsync(cancellationToken);
    }

    public Task<MessageListDto[]> GetMessages(
        GetMessagesRequest request,
        CancellationToken cancellationToken)
    {
        return context
            .Messages
            .Where(x => x.UserId == request.UserId)
            .Where(x => x.CategoryId == request.CategoryId)
            .OrderByDescending(x => x.Id)
            .Select(x => new MessageListDto
            {
                Id = x.Id,
                Sender = x.Sender,
                Text = x.Content,
                Time = x.CreatedAt
            })
            .ToArrayAsync(cancellationToken);
    }
}

public record GetBacklogMessagesRequest
{
    public Guid UserId { get; set; }
}

public record GetMessagesRequest
{
    public Guid UserId { get; set; }
    public long CategoryId { get; set; }
}

public class MessageListDto
{
    public required long Id { get; set; }
    public required DateTime Time { get; set; }
    public required string? Sender { get; set; }
    public string? SenderInitial => Sender?[..2];
    public required string Text { get; set; }
}