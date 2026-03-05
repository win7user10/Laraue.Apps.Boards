using Laraue.Apps.StructuredMessages.Services;
using Telegram.Bot;

namespace Laraue.Apps.StructuredMessages.TelegramServices;

public interface ITelegramMessageService
{
    Task SaveMessage(
        SaveTelegramMessageRequest request,
        CancellationToken cancellationToken);
}

public class TelegramMessageService(
    IMessageService messageService,
    ITelegramBotClient client)
    : ITelegramMessageService
{
    public async Task SaveMessage(
        SaveTelegramMessageRequest request,
        CancellationToken cancellationToken)
    {
        var id = await messageService.SaveMessage(
            new SaveMessageRequest
            {
                UserId = request.UserId,
                Text = request.Text,
                CreatedAt = request.SentAt,
            },
            cancellationToken);

        var responseText = $"Saved ({id}): {request.Text}";
        
        await client.SendMessage(
            request.TelegramUserId,
            responseText,
            cancellationToken: cancellationToken);
    }
}

public class SaveTelegramMessageRequest
{
    public required long TelegramUserId { get; set; }
    public required Guid UserId { get; set; }
    public required string Text { get; set; }
    public required DateTime SentAt { get; set; }
}