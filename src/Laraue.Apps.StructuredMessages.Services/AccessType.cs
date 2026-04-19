namespace Laraue.Apps.StructuredMessages.Services;

[Flags]
public enum AccessType : byte
{
    Read = 1,
    Create = 2,
    Update = 4,
    Delete = 8,
    Manage = 16,
}