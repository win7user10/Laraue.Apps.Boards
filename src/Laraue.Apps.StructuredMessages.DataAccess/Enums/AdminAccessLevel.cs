namespace Laraue.Apps.StructuredMessages.DataAccess.Enums;

[Flags]
public enum AdminAccessLevel : byte
{
    None = 0,
    CreateSpaces = 1,
    ManagePermissions = 2,
    UpdateOrganization = 4,
    DeleteOrganization = 8,
    All = CreateSpaces | ManagePermissions | DeleteOrganization | UpdateOrganization,
}