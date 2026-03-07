namespace Laraue.Apps.StructuredMessages.TelegramServices.Services.Messages;

public class HandleCreateStatusFromMessageRequest
{
    public required long MessageCategoryId { get; set; }
    public required long TelegramUserId { get; set; }
    public required long MessageId { get; set; }
    public required Guid UserId { get; set; }
}