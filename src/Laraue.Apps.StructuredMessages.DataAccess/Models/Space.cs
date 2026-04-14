using System.ComponentModel.DataAnnotations;

namespace Laraue.Apps.StructuredMessages.DataAccess.Models;

public class Space
{
    public long Id { get; set; }
    
    [MaxLength(128)]
    public required string Name { get; set; }
    
    [MaxLength(7)]
    public string? Color { get; set; }
    
    public Guid CreatorId { get; set; }
    public User? Creator { get; set; }
    
    /// <summary>
    /// Epic creation date.
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Epic attribute update date.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}