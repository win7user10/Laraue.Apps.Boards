using Laraue.Apps.StructuredMessages.DataAccess.Enums;

namespace Laraue.Apps.StructuredMessages.DataAccess.Models;

public class OrganizationUser
{
    public long Id { get; set; }
    
    public long OrganizationId { get; set; }
    public Organization? Organization { get; set; }
    
    public Guid UserId { get; set; }
    public User? User { get; set; }
    
    public ChildrenAccessLevel SpacesAccessLevel { get; set; }
    public ChildrenAccessLevel EpicsAccessLevel { get; set; }
    public ChildrenAccessLevel IssuesAccessLevel { get; set; }
    
    public AdminAccessLevel AdminAccessLevel { get; set; }
}