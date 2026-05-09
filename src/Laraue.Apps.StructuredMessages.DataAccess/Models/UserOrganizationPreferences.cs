namespace Laraue.Apps.StructuredMessages.DataAccess.Models;

public class UserOrganizationPreferences
{
    public Guid UserId { get; set; }
    public long OrganizationId { get; set; }
    
    /// <summary>
    /// Last selected space id.
    /// </summary>
    public long? SelectedSpaceId { get; set; }
}