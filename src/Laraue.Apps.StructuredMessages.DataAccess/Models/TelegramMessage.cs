namespace Laraue.Apps.StructuredMessages.DataAccess.Models;

public class TelegramMessage
{
    /// <summary>
    /// Telegram message identifier.
    /// </summary>
    public int Id { get; init; }
    
    public long? TelegramMediaGroupId { get; init; }
    public TelegramMediaGroup? TelegramMediaGroup { get; set; }
    
    /// <summary>
    /// The card related to this message
    /// </summary>
    public Card? Card { get; set; }
}