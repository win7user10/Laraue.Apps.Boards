namespace Laraue.Apps.StructuredMessages.TelegramServices;

public static class TelegramRoutes
{
    public const string Category = "c/{id}";
    public const string SetMessageCategory = "m/sc";
    
    public const string Categories = "/categories";
    public const string CreateCategory = "/categories add {name}";
}