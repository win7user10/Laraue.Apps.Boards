using System.ComponentModel.DataAnnotations;

namespace Laraue.Apps.StructuredMessages.DataAccess.Models;

public class MessageCategoryStatus
{
    public long Id { get; set; }
    
    [MaxLength(128)]
    public required string Name { get; set; }
    
    [MaxLength(7)]
    public string? Color { get; set; }
    
    public long MessageCategoryId { get; set; }
    public MessageCategory? MessageCategory { get; set; }

    public bool IsFinal { get; set; }
}