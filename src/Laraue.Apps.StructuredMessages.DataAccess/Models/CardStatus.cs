using System.ComponentModel.DataAnnotations;

namespace Laraue.Apps.StructuredMessages.DataAccess.Models;

public class CardStatus
{
    public long Id { get; set; }

    [MaxLength(128)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(7)]
    public string? Color { get; set; }
    
    public long CardCategoryId { get; set; }
    public CardCategory? CardCategory { get; set; }

    public int SortOrder { get; set; }
}