namespace Laraue.Apps.StructuredMessages.DataAccess.Enums;

[Flags]
public enum AccessLevel : byte
{
    Read = 1,
    Create = 2,
    Update = 4,
    Delete = 8,
    Manage = 16,
}