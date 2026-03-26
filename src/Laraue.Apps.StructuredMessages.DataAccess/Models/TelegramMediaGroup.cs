using System.ComponentModel.DataAnnotations;

namespace Laraue.Apps.StructuredMessages.DataAccess.Models;

public class TelegramMediaGroup
{
    public long Id { get; init; }
    
    /// <summary>
    /// When the file contains messages this field allow to attachments in one message.
    /// </summary>
    [MaxLength(64)]
    public string? ExternalId { get; set; }
}