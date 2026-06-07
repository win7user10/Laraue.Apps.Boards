namespace Laraue.Apps.Boards.DataAccess.Models;

public class UserPreferences
{
    public Guid UserId { get; set; }
    
    /// <summary>
    /// Last selected borders ordering.
    /// </summary>
    public EpicSortOrder EpicSortOrder { get; set; }
}

public enum EpicSortOrder
{
    LastTouched,
    Alphabetical,
}