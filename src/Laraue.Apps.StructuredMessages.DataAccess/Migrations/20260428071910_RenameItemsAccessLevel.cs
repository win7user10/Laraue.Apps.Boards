using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Laraue.Apps.StructuredMessages.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RenameItemsAccessLevel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "items_access_level",
                table: "space_organization_users",
                newName: "item_access_level");

            migrationBuilder.RenameColumn(
                name: "items_access_level",
                table: "organization_users",
                newName: "item_access_level");

            migrationBuilder.RenameColumn(
                name: "items_access_level",
                table: "epic_organization_users",
                newName: "item_access_level");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "item_access_level",
                table: "space_organization_users",
                newName: "items_access_level");

            migrationBuilder.RenameColumn(
                name: "item_access_level",
                table: "organization_users",
                newName: "items_access_level");

            migrationBuilder.RenameColumn(
                name: "item_access_level",
                table: "epic_organization_users",
                newName: "items_access_level");
        }
    }
}
