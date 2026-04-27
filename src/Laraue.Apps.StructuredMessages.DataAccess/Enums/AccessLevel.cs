namespace Laraue.Apps.StructuredMessages.DataAccess.Enums;

[Flags]
public enum AccessLevel : byte
{
    None = 0,
    ReadItems = 1,
    CreateItems = 2,
    UpdateItems = 4,
    DeleteItems = 8,
    All = ReadItems | CreateItems | UpdateItems | DeleteItems
}