namespace Laraue.Apps.StructuredMessages.TelegramServices.Services.Messages;

public interface ITelegramMessageService
{
    Task HandleSaveMessage(
        SaveMessageTelegramRequest request,
        CancellationToken cancellationToken);
}