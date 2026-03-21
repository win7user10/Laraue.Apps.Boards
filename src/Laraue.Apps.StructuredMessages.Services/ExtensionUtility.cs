using System.Diagnostics.CodeAnalysis;

namespace Laraue.Apps.StructuredMessages.Services;

public class ExtensionUtility
{
    public static string? GetExtension([NotNullIfNotNull(nameof(mimeType))] string? mimeType)
    {
        return mimeType?.Split("/")[1];
    }
}