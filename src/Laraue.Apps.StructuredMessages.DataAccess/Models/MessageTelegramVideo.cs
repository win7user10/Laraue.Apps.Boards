namespace Laraue.Apps.StructuredMessages.DataAccess.Models;

public class MessageTelegramVideo
{
    public long Id { get; set; }
    
    public Card? Message { get; set; }
    public long MessageId { get; set; }
    
    public int Width { get; set; }
    public int Height { get; set; }
    
    public Guid FileId { get; set; }
    public TelegramFile? File { get; set; }
    
    // Thumbnail section
    public int? ThumbnailWidth { get; set; }
    public int? ThumbnailHeight { get; set; }
    public Guid? ThumbnailFileId { get; set; }
    public TelegramFile? ThumbnailFile { get; set; }
}