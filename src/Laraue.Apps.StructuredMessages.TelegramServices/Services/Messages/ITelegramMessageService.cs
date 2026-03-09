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
    
    Task OpenChangeCategoryWindow(
        Guid userId,
        int? editMessageId,
        HandleOpenChangeCategoryWindowRequest request,
        CancellationToken cancellationToken);
    
    Task HandleCreateCategory(
        HandleCreateCategoryFromMessageRequest request,
        CancellationToken cancellationToken);
    
    Task OpenChangeStatusWindow(
        Guid userId,
        int? editMessageId,
        HandleOpenChangeStatusWindowRequest request,
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