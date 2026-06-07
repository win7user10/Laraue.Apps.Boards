using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Laraue.Apps.StructuredMessages.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class DropEpicDirectPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "direct_epic_permissions");
            
            migrationBuilder.RenameColumn(
                name: "entity_access_level",
                table: "direct_space_permissions",
                newName: "issues_access_level");

            migrationBuilder.RenameColumn(
                name: "children_issues_access_level",
                table: "direct_space_permissions",
                newName: "epics_access_level");

            migrationBuilder.RenameColumn(
                name: "children_epics_access_level",
                table: "direct_space_permissions",
                newName: "access_level");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "direct_epic_permissions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    epic_id = table.Column<long>(type: "bigint", nullable: false),
                    organization_user_id = table.Column<long>(type: "bigint", nullable: false),
                    children_issues_access_level = table.Column<byte>(type: "smallint", nullable: false),
                    entity_access_level = table.Column<byte>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_direct_epic_permissions", x => x.id);
                    table.ForeignKey(
                        name: "fk_direct_epic_permissions_epics_epic_id",
                        column: x => x.epic_id,
                        principalTable: "epics",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_direct_epic_permissions_organization_users_organization_use",
                        column: x => x.organization_user_id,
                        principalTable: "organization_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_direct_epic_permissions_epic_id",
                table: "direct_epic_permissions",
                column: "epic_id");

            migrationBuilder.CreateIndex(
                name: "ix_direct_epic_permissions_organization_user_id",
                table: "direct_epic_permissions",
                column: "organization_user_id");
            
            migrationBuilder.RenameColumn(
                name: "issues_access_level",
                table: "direct_space_permissions",
                newName: "entity_access_level");

            migrationBuilder.RenameColumn(
                name: "epics_access_level",
                table: "direct_space_permissions",
                newName: "children_issues_access_level");

            migrationBuilder.RenameColumn(
                name: "access_level",
                table: "direct_space_permissions",
                newName: "children_epics_access_level");
        }
    }
}
