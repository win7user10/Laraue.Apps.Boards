using System.ComponentModel.DataAnnotations;

namespace Laraue.Apps.StructuredMessages.DataAccess.Models;

public class MessageTypeStatus
{
    public long Id { get; set; }
    
    [MaxLength(128)]
    public required string Name { get; set; }
    
    public long MessageTypeId { get; set; }
    public MessageType? MessageType { get; set; }

    public bool IsFinal { get; set; }
}