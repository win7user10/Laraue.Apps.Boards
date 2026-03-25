using System.ComponentModel.DataAnnotations;

namespace Laraue.Apps.StructuredMessages.DataAccess.Models;

public class CardCategory
{
    public long Id { get; set; }
    
    [MaxLength(128)]
    public required string Name { get; set; }
    
    [MaxLength(7)]
    public string? Color { get; set; }
    
    public Guid UserId { get; set; }
    public User? User { get; set; }
    
    public IList<Card>? Cards { get; set; }
    public IList<CardStatus>? Statuses { get; set; }
}