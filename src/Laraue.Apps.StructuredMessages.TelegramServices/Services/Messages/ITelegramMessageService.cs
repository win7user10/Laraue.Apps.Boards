namespace Laraue.Apps.StructuredMessages.TelegramServices.Services.Messages;

public interface ITelegramMessageService
{
    Task HandleSaveMessage(
        SaveMessageTelegramRequest request,
        CancellationToken cancellationToken);
    
    Task HandleUpdateMessageCategory(
        ReplyData replyData,
        UpdateMessageCategoryTelegramRequest request,
        CancellationToken cancellationToken);
    
    Task HandleCreateCategory(
        HandleCreateCategoryFromMessageRequest fromMessageRequest,
        CancellationToken cancellationToken);
    
    Task SendMessageSaved(
        SendMessageSavedRequest request,
        CancellationToken cancellationToken);
}