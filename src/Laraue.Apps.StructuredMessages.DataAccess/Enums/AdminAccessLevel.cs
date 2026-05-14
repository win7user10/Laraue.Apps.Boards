namespace Laraue.Apps.StructuredMessages.DataAccess.Enums;

[Flags]
public enum AdminAccessLevel : byte
{
    None = 0,
    Manage = 1,
    UpdateOrganization = 2,
    DeleteOrganization = 4,
    All = Manage | UpdateOrganization | DeleteOrganization,
}