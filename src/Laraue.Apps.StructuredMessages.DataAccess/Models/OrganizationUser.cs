using Laraue.Apps.StructuredMessages.DataAccess.Enums;

namespace Laraue.Apps.StructuredMessages.DataAccess.Models;

public class OrganizationUser
{
    public long Id { get; set; }
    
    public long OrganizationId { get; set; }
    public Organization? Organization { get; set; }
    
    public Guid UserId { get; set; }
    public User? User { get; set; }
    
    public ItemAccessLevel ItemAccessLevel { get; set; }
    public AdminAccessLevel AdminAccessLevel { get; set; }
}