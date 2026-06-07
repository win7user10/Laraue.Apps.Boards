namespace Laraue.Apps.Boards.TelegramServices.Services.Messages;

public class HandleCreateStatusFromMessageRequest
{
    public required long MessageCategoryId { get; set; }
    public required long MessageId { get; set; }
    public required ReplyData ReplyData { get; set; }
}