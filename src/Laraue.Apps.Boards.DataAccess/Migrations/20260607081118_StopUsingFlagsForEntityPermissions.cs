using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Laraue.Apps.StructuredMessages.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class StopUsingFlagsForEntityPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "can_create_epics",
                table: "organization_users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "can_create_issues",
                table: "organization_users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "can_create_spaces",
                table: "organization_users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "can_delete_epics",
                table: "organization_users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "can_delete_issues",
                table: "organization_users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "can_delete_spaces",
                table: "organization_users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "can_read",
                table: "organization_users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "can_update_epics",
                table: "organization_users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "can_update_issues",
                table: "organization_users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "can_update_spaces",
                table: "organization_users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "can_create_epics",
                table: "direct_space_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "can_create_issues",
                table: "direct_space_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "can_delete",
                table: "direct_space_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "can_delete_epics",
                table: "direct_space_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "can_delete_issues",
                table: "direct_space_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "can_read",
                table: "direct_space_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "can_update",
                table: "direct_space_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "can_update_epics",
                table: "direct_space_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "can_update_issues",
                table: "direct_space_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(@"
update organization_users
set 
    can_create_issues = (issues_access_level & 2) = 2 OR (epics_access_level & 2) = 2 OR (spaces_access_level & 2) = 2,
    can_create_epics = (epics_access_level & 2) = 2 OR (spaces_access_level & 2) = 2,
    can_create_spaces = (spaces_access_level & 2) = 2,
    
    can_delete_issues = (issues_access_level & 8) = 8 OR (epics_access_level & 8) = 8 OR (spaces_access_level & 8) = 8,
    can_delete_epics = (epics_access_level & 8) = 8 OR (spaces_access_level & 8) = 8,
    can_delete_spaces = (spaces_access_level & 8) = 8,
    
    can_update_epics = (issues_access_level & 4) = 4 OR (epics_access_level & 4) = 4 OR (spaces_access_level & 4) = 4,
    can_update_issues = (issues_access_level & 4) = 4 OR (spaces_access_level & 4) = 4,
    can_update_spaces = (spaces_access_level & 4) = 4,
    
    can_read = (spaces_access_level & 1) = 1 OR (epics_access_level & 1) = 1 OR (issues_access_level & 1) = 1
");

            migrationBuilder.Sql(@"
update direct_space_permissions dsp
set 
    can_create_issues = ou.can_create_issues OR ((dsp.issues_access_level & 2) = 2 OR (dsp.epics_access_level & 2) = 2 OR (dsp.access_level & 2) = 2),
    can_create_epics = ou.can_create_epics OR (dsp.epics_access_level & 2) = 2 OR (dsp.access_level & 2) = 2,
    
    can_delete_issues = ou.can_delete_issues OR ((dsp.issues_access_level & 8) = 8 OR (dsp.epics_access_level & 8) = 8 OR (dsp.access_level & 8) = 8),
    can_delete_epics = ou.can_delete_epics OR (dsp.epics_access_level & 8) = 8 OR (dsp.access_level & 8) = 8,
    can_delete = ou.can_delete_spaces OR (dsp.access_level & 8) = 8,
    
    can_update_issues = ou.can_update_issues OR ((dsp.issues_access_level & 4) = 4 OR (dsp.epics_access_level & 4) = 4 OR (dsp.access_level & 4) = 4),
    can_update_epics = ou.can_update_epics OR (dsp.epics_access_level & 4) = 4 OR (dsp.access_level & 4) = 4,
    can_update = ou.can_update_spaces OR (dsp.access_level & 4) = 4,
    
    can_read = ou.can_read OR (dsp.access_level & 1) = 1 OR (dsp.epics_access_level & 1) = 1 OR (dsp.issues_access_level & 1) = 1
FROM organization_users ou
WHERE ou.id = dsp.organization_user_id;");
            
            migrationBuilder.DropColumn(
                name: "epics_access_level",
                table: "organization_users");

            migrationBuilder.DropColumn(
                name: "issues_access_level",
                table: "organization_users");

            migrationBuilder.DropColumn(
                name: "spaces_access_level",
                table: "organization_users");

            migrationBuilder.DropColumn(
                name: "access_level",
                table: "direct_space_permissions");

            migrationBuilder.DropColumn(
                name: "epics_access_level",
                table: "direct_space_permissions");

            migrationBuilder.DropColumn(
                name: "issues_access_level",
                table: "direct_space_permissions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "can_create_epics",
                table: "organization_users");

            migrationBuilder.DropColumn(
                name: "can_create_issues",
                table: "organization_users");

            migrationBuilder.DropColumn(
                name: "can_create_spaces",
                table: "organization_users");

            migrationBuilder.DropColumn(
                name: "can_delete_epics",
                table: "organization_users");

            migrationBuilder.DropColumn(
                name: "can_delete_issues",
                table: "organization_users");

            migrationBuilder.DropColumn(
                name: "can_delete_spaces",
                table: "organization_users");

            migrationBuilder.DropColumn(
                name: "can_read",
                table: "organization_users");

            migrationBuilder.DropColumn(
                name: "can_update_epics",
                table: "organization_users");

            migrationBuilder.DropColumn(
                name: "can_update_issues",
                table: "organization_users");

            migrationBuilder.DropColumn(
                name: "can_update_spaces",
                table: "organization_users");

            migrationBuilder.DropColumn(
                name: "can_create_epics",
                table: "direct_space_permissions");

            migrationBuilder.DropColumn(
                name: "can_create_issues",
                table: "direct_space_permissions");

            migrationBuilder.DropColumn(
                name: "can_delete",
                table: "direct_space_permissions");

            migrationBuilder.DropColumn(
                name: "can_delete_epics",
                table: "direct_space_permissions");

            migrationBuilder.DropColumn(
                name: "can_delete_issues",
                table: "direct_space_permissions");

            migrationBuilder.DropColumn(
                name: "can_read",
                table: "direct_space_permissions");

            migrationBuilder.DropColumn(
                name: "can_update",
                table: "direct_space_permissions");

            migrationBuilder.DropColumn(
                name: "can_update_epics",
                table: "direct_space_permissions");

            migrationBuilder.DropColumn(
                name: "can_update_issues",
                table: "direct_space_permissions");

            migrationBuilder.AddColumn<byte>(
                name: "epics_access_level",
                table: "organization_users",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<byte>(
                name: "issues_access_level",
                table: "organization_users",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<byte>(
                name: "spaces_access_level",
                table: "organization_users",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<byte>(
                name: "access_level",
                table: "direct_space_permissions",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<byte>(
                name: "epics_access_level",
                table: "direct_space_permissions",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<byte>(
                name: "issues_access_level",
                table: "direct_space_permissions",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0);
        }
    }
}
