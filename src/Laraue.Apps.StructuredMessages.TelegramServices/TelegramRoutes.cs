namespace Laraue.Apps.StructuredMessages.TelegramServices;

public static class TelegramRoutes
{
    public const string Category = "c/{id}";
    public const string SetMessageCategory = "m/sc";
    public const string SetMessageStatus = "m/ss";
    
    public const string Categories = "/categories";
    public const string CreateCategoryFromMessage = "ccfm";
    public const string CreateStatusFromMessage = "csfm";
    
    public const string UpdateMessageText = "m/ut";
}