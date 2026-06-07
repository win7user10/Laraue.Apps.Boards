namespace Laraue.Apps.Boards.TelegramServices.Services.Messages;

public abstract class SaveMessageTelegramRequest
{
    public required string? From { get; set; }
    public required long ExternalUserId { get; set; }
    public required int ExternalMessageId { get; set; }
    public required Guid UserId { get; set; }
    public required string? Text { get; set; }
    public required DateTime SentAt { get; set; }
    public required string? MediaGroupId { get; set; }
}

public class SaveTextMessageTelegramRequest : SaveMessageTelegramRequest
{}

public class SaveImageMessageTelegramRequest : SaveMessageTelegramRequest
{
    public required PhotoSize[] Photos { get; set; }
}

public class SaveVideoMessageTelegramRequest : SaveMessageTelegramRequest
{
    public required File Video { get; set; }
    public required PhotoSize? Thumbnail { get; set; }
    public required int Width { get; set; }
    public required int Height { get; set; }
    public required int Duration { get; set; }
}

public class PhotoSize : File
{
    public required int Width { get; set; }
    public required int Height { get; set; }
}

public class File
{
    public required long? FileSize { get; set; }
    public required string FileId { get; set; }
    public required string FileUniqueId { get; set; }
    public required string? FileName { get; set; }
    public required string? MimeType { get; set; }
}