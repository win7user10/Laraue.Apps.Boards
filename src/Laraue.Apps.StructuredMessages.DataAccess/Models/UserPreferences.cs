namespace Laraue.Apps.StructuredMessages.DataAccess.Models;

public class UserPreferences
{
    public Guid UserId { get; set; }
    
    /// <summary>
    /// Last selected borders ordering.
    /// </summary>
    public EpicSortOrder EpicSortOrder { get; set; }
    
    /// <summary>
    /// Last selected space id.
    /// </summary>
    public long SpaceId { get; set; }
}

public enum EpicSortOrder
{
    LastTouched,
    Alphabetical,
}