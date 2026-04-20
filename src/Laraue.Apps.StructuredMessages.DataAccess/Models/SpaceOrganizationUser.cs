using Laraue.Apps.StructuredMessages.DataAccess.Enums;

namespace Laraue.Apps.StructuredMessages.DataAccess.Models;

public class SpaceOrganizationUser
{
    public long Id { get; set; }
    
    /// <summary>
    /// When space is not set than permission is applied for all spaces.
    /// </summary>
    public long? SpaceId { get; set; }
    public Space? Space { get; set; }
    
    public long OrganizationUserId { get; set; }
    public OrganizationUser? OrganizationUser { get; set; }
    
    public AccessLevel AccessLevel { get; set; }
}