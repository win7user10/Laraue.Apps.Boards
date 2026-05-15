using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Laraue.Apps.StructuredMessages.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "space_id",
                table: "user_preferences");

            migrationBuilder.CreateTable(
                name: "user_organization_preferences",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<long>(type: "bigint", nullable: false),
                    selected_space_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_organization_preferences", x => new { x.organization_id, x.user_id });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_organization_preferences");

            migrationBuilder.AddColumn<long>(
                name: "space_id",
                table: "user_preferences",
                type: "bigint",
                nullable: true);
        }
    }
}
