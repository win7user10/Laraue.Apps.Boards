namespace Laraue.Apps.StructuredMessages.DataAccess.Models;

public class TelegramMessageVideo
{
    public long Id { get; set; }
    
    public TelegramMessage? TelegramMessage { get; set; }
    public long TelegramMessageId { get; set; }
    
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