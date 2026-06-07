namespace Laraue.Apps.Boards.TelegramServices.Services.Messages;

public class HandleCreateCategoryFromMessageRequest
{
    public required string? From { get; set; }
    public required long MessageId { get; set; }
    public required ReplyData ReplyData { get; set; }
}