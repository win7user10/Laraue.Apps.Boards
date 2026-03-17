namespace Laraue.Apps.StructuredMessages.Services;

public class InterfaceLanguage
{
    public required string Code { get; init; }

    public required string Title { get; init; }

    public static InterfaceLanguage[] Available { get; } =
    [
        new() { Code = "en", Title = "English" },
        new() { Code = "ru", Title = "Русский" },
    ];

    public static InterfaceLanguage Default => Available[0];

    public static InterfaceLanguage ForCode(string? code)
    {
        return Available.FirstOrDefault(x => x.Code == code) ?? Default;
    }
}