using System.ComponentModel.DataAnnotations;

namespace Laraue.Apps.StructuredMessages.DataAccess.Models;

public class Epic
{
    public long Id { get; set; }
    
    [MaxLength(128)]
    public required string Name { get; set; }
    
    [MaxLength(7)]
    public string? Color { get; set; }
    
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public IList<Issue>? Issues { get; set; }
    public IList<Status>? Statuses { get; set; }
}