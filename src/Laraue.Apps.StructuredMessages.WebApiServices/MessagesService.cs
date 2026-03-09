using Laraue.Apps.StructuredMessages.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Laraue.Apps.StructuredMessages.WebApiServices;

public interface IMessagesService
{
    Task<MessageListDto[]> GetMessages(
        GetMessagesRequest request,
        CancellationToken cancellationToken);
}

public class MessagesService(DatabaseContext context) : IMessagesService
{
    public Task<MessageListDto[]> GetMessages(
        GetMessagesRequest request,
        CancellationToken cancellationToken)
    {
        return context
            .Messages
            .Where(x => x.UserId == request.UserId)
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

public class GetMessagesRequest
{
    public Guid UserId { get; set; }
}

public class MessageListDto
{
    public required long Id { get; set; }
    public required DateTime Time { get; set; }
    public required string? Sender { get; set; }
    public char? SenderInitial => Sender?.FirstOrDefault();
    public required string Text { get; set; }
}