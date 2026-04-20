using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Laraue.Apps.StructuredMessages.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class SpaceOrganizationPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "space_organization_users",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    space_id = table.Column<long>(type: "bigint", nullable: true),
                    organization_user_id = table.Column<long>(type: "bigint", nullable: false),
                    access_level = table.Column<byte>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_space_organization_users", x => x.id);
                    table.ForeignKey(
                        name: "fk_space_organization_users_organization_users_organization_us",
                        column: x => x.organization_user_id,
                        principalTable: "organization_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_space_organization_users_spaces_space_id",
                        column: x => x.space_id,
                        principalTable: "spaces",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_space_organization_users_organization_user_id",
                table: "space_organization_users",
                column: "organization_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_space_organization_users_space_id",
                table: "space_organization_users",
                column: "space_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "space_organization_users");
        }
    }
}
