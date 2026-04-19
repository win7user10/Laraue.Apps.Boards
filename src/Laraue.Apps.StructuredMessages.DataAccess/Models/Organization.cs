using System.ComponentModel.DataAnnotations;

namespace Laraue.Apps.StructuredMessages.DataAccess.Models;

/// <summary>
/// The unit that can has spaces, boards, issues.
/// </summary>
public class Organization
{
    public long Id { get; set; }
    
    [MaxLength(128)]
    public required string Name { get; set; }
    
    [MaxLength(7)]
    public string? Color { get; set; }
    
    /// <summary>
    /// Who has the full space permissions.
    /// </summary>
    public Guid OwnerId { get; set; }
    public User? Owner { get; set; }
    
    /// <summary>
    /// Type. Organization can be a personal (single user) or organization (multiple users).
    /// </summary>
    public OrganizationType Type { get; set; }
    
    /// <summary>
    /// Epic creation date.
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Epic attribute update date.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
    
    /// <summary>
    /// Spaces linked to the organization.
    /// </summary>
    public IList<Space>? Spaces { get; set; }
}

public enum OrganizationType
{
    Personal,
    Organization,
}