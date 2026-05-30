namespace Laraue.Apps.StructuredMessages.DataAccess.Models;

/// <summary>
/// Issue serial number inside the space.
/// </summary>
public class IssueNumber
{
    public long IssueId { get; set; }
    public Issue? Issue { get; set; }
    
    public long SpaceId { get; set; }
    public Space? Space { get; set; }
    
    public int Number { get; set; }
}