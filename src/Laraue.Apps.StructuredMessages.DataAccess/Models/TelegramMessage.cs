namespace Laraue.Apps.StructuredMessages.DataAccess.Models;

public class TelegramMessage
{
    public long Id { get; set; }

    /// <summary>
    /// Telegram message identifier.
    /// </summary>
    public required int TelegramMessageId { get; init; }
    public required long TelegramChatId { get; init; }
    
    public long? TelegramMediaGroupId { get; init; }
    public TelegramMediaGroup? TelegramMediaGroup { get; set; }
    
    /// <summary>
    /// The card related to this message
    /// </summary>
    public Card? Card { get; set; }
}