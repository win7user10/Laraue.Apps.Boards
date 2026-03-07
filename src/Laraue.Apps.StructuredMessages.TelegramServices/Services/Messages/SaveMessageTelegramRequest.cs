namespace Laraue.Apps.StructuredMessages.TelegramServices.Services.Messages;

public class SaveMessageTelegramRequest
{
    public required string? From { get; set; }
    public required long TelegramUserId { get; set; }
    public required int TelegramMessageId { get; set; }
    public required Guid UserId { get; set; }
    public required string Text { get; set; }
    public required DateTime SentAt { get; set; }
}