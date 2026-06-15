namespace Laraue.Apps.Boards.DataAccess.Models;

/// <summary>
/// One of the list attribute possible value.
/// </summary>
public class AttributeListValue
{
    public long Id { get; set; }
    
    public long AttributeId { get; set; }
    public Attribute? Attribute { get; set; }
    
    public required string Value { get; set; }
}