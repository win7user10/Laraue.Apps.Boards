using Laraue.Apps.StructuredMessages.DataAccess.Enums;

namespace Laraue.Apps.StructuredMessages.DataAccess.Models;

public class EpicOrganizationUser
{
    public long Id { get; set; }
    
    /// <summary>
    /// When epic is not set than permission is applied for all epics.
    /// </summary>
    public long? EpicId { get; set; }
    public Epic? Epic { get; set; }
    
    public long OrganizationUserId { get; set; }
    public OrganizationUser? OrganizationUser { get; set; }
    
    public ItemAccessLevel ItemAccessLevel { get; set; }
}