namespace Laraue.Apps.StructuredMessages.DataAccess.Enums;

public enum AccessLevel : byte
{
    None = 0,
    Read = 1,
    Create = 2,
    Update = 4,
    Delete = 8,
    Manage = 16,
}