namespace Laraue.Apps.StructuredMessages.DataAccess.Enums;

[Flags]
public enum ItemAccessLevel : byte
{
    None = 0,
    ReadItems = 1,
    CreateItems = 2,
    UpdateSelf = 4,
    DeleteSelf = 8,
    All = ReadItems | CreateItems | UpdateSelf | DeleteSelf
}