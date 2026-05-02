using Laraue.Apps.StructuredMessages.DataAccess.Enums;

namespace Laraue.Apps.StructuredMessages.DataAccess.Models;

public class DirectEpicPermission
{
    public long Id { get; set; }
    
    public long EpicId { get; set; }
    public Epic? Epic { get; set; }
    
    public long OrganizationUserId { get; set; }
    public OrganizationUser? OrganizationUser { get; set; }
    
    public ChildrenAccessLevel ChildrenIssuesAccessLevel { get; set; }
    public EntityAccessLevel EntityAccessLevel { get; set; }
}