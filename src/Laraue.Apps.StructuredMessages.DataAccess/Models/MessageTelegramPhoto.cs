namespace Laraue.Apps.StructuredMessages.DataAccess.Models;

public class MessageTelegramPhoto
{
    public long Id { get; set; }
    
    public Message? Message { get; set; }
    public long MessageId { get; set; }
    
    public int Width { get; set; }
    public int Height { get; set; }
    
    public Guid TelegramFileId { get; set; }
    public TelegramFile? File { get; set; }
}