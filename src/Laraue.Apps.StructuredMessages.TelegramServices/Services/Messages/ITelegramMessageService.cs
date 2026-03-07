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
        HandleCreateCategoryFromMessageRequest request,
        CancellationToken cancellationToken);
    
    Task HandleUpdateMessageStatus(
        ReplyData replyData,
        UpdateMessageStatusTelegramRequest request,
        CancellationToken cancellationToken);
    
    Task HandleCreateStatus(
        HandleCreateStatusFromMessageRequest request,
        CancellationToken cancellationToken);
    
    Task SendMessageToChat(
        long messageId,
        CancellationToken cancellationToken);
}