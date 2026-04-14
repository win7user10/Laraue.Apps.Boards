namespace Laraue.Apps.StructuredMessages.Services;

[Flags]
public enum AccessType
{
    Read = 0,
    Create = 1,
    Update = 2,
    Delete = 4
}