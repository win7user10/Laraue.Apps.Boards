using System.ComponentModel.DataAnnotations;

namespace Laraue.Apps.StructuredMessages.DataAccess.Models;

public class Message
{
    public long Id { get; set; }
    
    /// <summary>
    /// Message content.
    /// </summary>
    [MaxLength(4096)]
    public required string Content { get; set; }
    
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
    public MessageCategory? Category { get; set; }

    /// <summary>
    /// Actual message status.
    /// </summary>
    public long? StatusId { get; set; }
    public MessageStatus? Status { get; set; }
    
    public int? TelegramMessageId { get; set; }
}