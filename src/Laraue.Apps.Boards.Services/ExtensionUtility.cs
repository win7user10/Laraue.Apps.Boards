using System.Diagnostics.CodeAnalysis;

namespace Laraue.Apps.Boards.Services;

public class ExtensionUtility
{
    public static string? GetExtension([NotNullIfNotNull(nameof(mimeType))] string? mimeType)
    {
        return mimeType?.Split("/")[1];
    }
}