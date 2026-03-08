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
    
    Task HandleUpdateStatus(
        ReplyData replyData,
        UpdateMessageStatusTelegramRequest request,
        CancellationToken cancellationToken);
    
    Task HandleCreateStatus(
        HandleCreateStatusFromMessageRequest request,
        CancellationToken cancellationToken);

    Task HandleChangeContent(
        ReplyData replyData,
        HandleChangeContentTelegramRequest request,
        CancellationToken cancellationToken);
    
    Task UpdateMessageInChat(
        long messageId,
        int? editMessageId,
        CancellationToken cancellationToken);
}