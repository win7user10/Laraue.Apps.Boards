using System.ComponentModel.DataAnnotations;

namespace Laraue.Apps.StructuredMessages.DataAccess.Models;

public class MessageStatus
{
    public long Id { get; set; }

    [MaxLength(128)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(7)]
    public string? Color { get; set; }
    
    public long MessageCategoryId { get; set; }
    public MessageCategory? MessageCategory { get; set; }

    public int SortOrder { get; set; }
}