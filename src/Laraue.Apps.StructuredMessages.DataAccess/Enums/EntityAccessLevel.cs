namespace Laraue.Apps.StructuredMessages.DataAccess.Enums;

[Flags]
public enum EntityAccessLevel : byte
{
    None = 0,
    Read = 1,
    Update = 4,
    Delete = 8,
    All = Read | Update | Delete
}