using System.ComponentModel.DataAnnotations;

namespace Laraue.Apps.Boards.DataAccess.Models;

public class IssueAttributeTextValue
{
    public long Id { get; set; }
    
    public long IssueId { get; set; }
    public Issue? Issue { get; set; }
    
    public long AttributeId { get; set; }
    public Attribute? Attribute { get; set; }
    
    [MaxLength(255)]
    public required string Text { get; set; }
}