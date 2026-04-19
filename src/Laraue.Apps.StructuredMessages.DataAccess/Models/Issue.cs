using System.ComponentModel.DataAnnotations;

namespace Laraue.Apps.StructuredMessages.DataAccess.Models;

public class Issue
{
    public long Id { get; set; }
    
    /// <summary>
    /// Message content.
    /// </summary>
    [MaxLength(4096)]
    public required string? Content { get; set; }
    
    /// <summary>
    /// Issue creation date.
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Issue update date.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
    
    public long? OrganizationId { get; set; }
    public Organization? Organization { get; set; }
    
    /// <summary>
    /// The user messages belongs to.
    /// </summary>
    public Guid UserId { get; set; }
    public User? User { get; set; }
    
    /// <summary>
    /// Message category.
    /// </summary>
    public long? EpicId { get; set; }
    public Epic? Epic { get; set; }
    
    /// <summary>
    /// The space the issue belongs to.
    /// </summary>
    public long? SpaceId { get; set; }
    public Space? Space { get; set; }

    /// <summary>
    /// Actual message status.
    /// </summary>
    public long? StatusId { get; set; }
    public Status? Status { get; set; }
    
    public TelegramMessage? TelegramMessage { get; set; }
    public long? TelegramMessageId { get; set; }
}