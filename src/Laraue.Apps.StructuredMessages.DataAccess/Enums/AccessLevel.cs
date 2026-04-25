namespace Laraue.Apps.StructuredMessages.DataAccess.Enums;

[Flags]
public enum AccessLevel : byte
{
    None = 0,
    Read = 1,
    Create = 2,
    Update = 4,
    Delete = 8,
    Manage = 16,
    All = Read | Create | Update | Delete | Manage
}