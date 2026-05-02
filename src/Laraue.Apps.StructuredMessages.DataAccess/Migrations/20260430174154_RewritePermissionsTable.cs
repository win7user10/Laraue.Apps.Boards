using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Laraue.Apps.StructuredMessages.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RewritePermissionsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "epic_organization_users");

            migrationBuilder.DropTable(
                name: "space_organization_users");

            migrationBuilder.RenameColumn(
                name: "item_access_level",
                table: "organization_users",
                newName: "spaces_access_level");

            migrationBuilder.AddColumn<byte>(
                name: "entity_access_level",
                table: "organization_users",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0);

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

            migrationBuilder.CreateTable(
                name: "direct_space_permissions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    space_id = table.Column<long>(type: "bigint", nullable: false),
                    organization_user_id = table.Column<long>(type: "bigint", nullable: false),
                    children_issues_access_level = table.Column<byte>(type: "smallint", nullable: false),
                    children_epics_access_level = table.Column<byte>(type: "smallint", nullable: false),
                    entity_access_level = table.Column<byte>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_direct_space_permissions", x => x.id);
                    table.ForeignKey(
                        name: "fk_direct_space_permissions_organization_users_organization_us",
                        column: x => x.organization_user_id,
                        principalTable: "organization_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_direct_space_permissions_spaces_space_id",
                        column: x => x.space_id,
                        principalTable: "spaces",
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

            migrationBuilder.CreateIndex(
                name: "ix_direct_space_permissions_organization_user_id",
                table: "direct_space_permissions",
                column: "organization_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_direct_space_permissions_space_id",
                table: "direct_space_permissions",
                column: "space_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "direct_epic_permissions");

            migrationBuilder.DropTable(
                name: "direct_space_permissions");

            migrationBuilder.DropColumn(
                name: "entity_access_level",
                table: "organization_users");

            migrationBuilder.DropColumn(
                name: "epics_access_level",
                table: "organization_users");

            migrationBuilder.DropColumn(
                name: "issues_access_level",
                table: "organization_users");

            migrationBuilder.RenameColumn(
                name: "spaces_access_level",
                table: "organization_users",
                newName: "item_access_level");

            migrationBuilder.CreateTable(
                name: "epic_organization_users",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    epic_id = table.Column<long>(type: "bigint", nullable: true),
                    organization_user_id = table.Column<long>(type: "bigint", nullable: false),
                    item_access_level = table.Column<byte>(type: "smallint", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "space_organization_users",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    organization_user_id = table.Column<long>(type: "bigint", nullable: false),
                    space_id = table.Column<long>(type: "bigint", nullable: true),
                    item_access_level = table.Column<byte>(type: "smallint", nullable: false)
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
                name: "ix_epic_organization_users_epic_id",
                table: "epic_organization_users",
                column: "epic_id");

            migrationBuilder.CreateIndex(
                name: "ix_epic_organization_users_organization_user_id",
                table: "epic_organization_users",
                column: "organization_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_space_organization_users_organization_user_id",
                table: "space_organization_users",
                column: "organization_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_space_organization_users_space_id",
                table: "space_organization_users",
                column: "space_id");
        }
    }
}
