using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Laraue.Apps.StructuredMessages.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class EpicOrganizationPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "epic_organization_users",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    epic_id = table.Column<long>(type: "bigint", nullable: true),
                    organization_user_id = table.Column<long>(type: "bigint", nullable: false),
                    access_level = table.Column<byte>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_epic_organization_users", x => x.id);
                    table.ForeignKey(
                        name: "fk_epic_organization_users_epics_epic_id",
                        column: x => x.epic_id,
                        principalTable: "epics",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_epic_organization_users_organization_users_organization_use",
                        column: x => x.organization_user_id,
                        principalTable: "organization_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_epic_organization_users_epic_id",
                table: "epic_organization_users",
                column: "epic_id");

            migrationBuilder.CreateIndex(
                name: "ix_epic_organization_users_organization_user_id",
                table: "epic_organization_users",
                column: "organization_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "epic_organization_users");
        }
    }
}
