using System.ComponentModel.DataAnnotations;

namespace Laraue.Apps.StructuredMessages.DataAccess.Models;

public class Card
{
    public long Id { get; set; }
    
    /// <summary>
    /// Message content.
    /// </summary>
    [MaxLength(4096)]
    public required string? Content { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// The user messages belongs to.
    /// </summary>
    public Guid UserId { get; set; }
    public User? User { get; set; }
    
    /// <summary>
    /// Message category.
    /// </summary>
    public long? CategoryId { get; set; }
    public CardCategory? Category { get; set; }

    /// <summary>
    /// Actual message status.
    /// </summary>
    public long? StatusId { get; set; }
    public CardStatus? Status { get; set; }
    
    public int? TelegramMessageId { get; set; }
    
    /// <summary>
    /// When the file contains messages this field allow to attachments in one message.
    /// </summary>
    [MaxLength(64)]
    public string? TelegramMediaGroupId { get; set; }
    
    /// <summary>
    /// Photos associated with a message.
    /// </summary>
    public IList<MessageTelegramPhoto>? Photos { get; set; }
    
    /// <summary>
    /// Videos associated with a message.
    /// </summary>
    public IList<MessageTelegramVideo>? Videos { get; set; }
}