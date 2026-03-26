namespace Laraue.Apps.StructuredMessages.DataAccess.Models;

public class TelegramMessagePhoto
{
    public long Id { get; set; }
    
    public TelegramMessage? TelegramMessage { get; set; }
    public int TelegramMessageId { get; set; }
    
    public int Width { get; set; }
    public int Height { get; set; }
    
    public Guid TelegramFileId { get; set; }
    public TelegramFile? File { get; set; }

    public PhotoType PhotoType { get; set; }
    
    // Photos of the one group are the same photo in different resolutions.
    public Guid GroupId { get; set; }
}

public enum PhotoType
{
    Thumbnail,
    Original,
}