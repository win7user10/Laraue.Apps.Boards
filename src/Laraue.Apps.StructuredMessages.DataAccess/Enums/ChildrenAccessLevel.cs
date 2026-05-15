namespace Laraue.Apps.StructuredMessages.DataAccess.Enums;

[Flags]
public enum ChildrenAccessLevel : byte
{
    None = 0,
    Read = 1,
    Create = 2,
    Update = 4,
    Delete = 8,
    All = Read | Create | Update | Delete
}

public static class ChildrenAccessLevelExtensions
{
    public static EntityAccessLevel ToEntityAccessLevel(this ChildrenAccessLevel accessLevel)
    {
        // Remove the Create flag (bit 2) because EntityAccessLevel does not have it
        var masked = (byte)accessLevel & ~(byte)ChildrenAccessLevel.Create;
        
        return (EntityAccessLevel)masked;
    }
}