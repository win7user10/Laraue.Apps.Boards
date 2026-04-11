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
    
    /// <summary>
    /// Epic creation date.
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Epic attribute update date.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
    
    /// <summary>
    /// When the epic or issues in epic were updated.
    /// Property to sort epics.
    /// </summary>
    public DateTime TouchedAt { get; set; }

    public IList<Issue>? Issues { get; set; }
    public IList<Status>? Statuses { get; set; }
}