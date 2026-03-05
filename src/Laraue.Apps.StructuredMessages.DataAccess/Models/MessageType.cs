using System.ComponentModel.DataAnnotations;

namespace Laraue.Apps.StructuredMessages.DataAccess.Models;

public class MessageType
{
    public long Id { get; set; }
    
    [MaxLength(128)]
    public required string Name { get; set; }
    
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
}