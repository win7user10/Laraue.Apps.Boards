using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Laraue.Apps.StructuredMessages.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "color",
                table: "users",
                type: "character varying(7)",
                maxLength: 7,
                nullable: false,
                defaultValue: "#3fb950");

            migrationBuilder.AlterColumn<string>(
                name: "color",
                table: "spaces",
                type: "character varying(7)",
                maxLength: 7,
                nullable: false,
                defaultValue: "#dda61b",
                oldClrType: typeof(string),
                oldType: "character varying(7)",
                oldMaxLength: 7,
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_default",
                table: "spaces",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "organization_id",
                table: "spaces",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AlterColumn<string>(
                name: "color",
                table: "epics",
                type: "character varying(7)",
                maxLength: 7,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(7)",
                oldMaxLength: 7,
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_default",
                table: "epics",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "organizations",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    join_code = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_organizations", x => x.id);
                    table.ForeignKey(
                        name: "fk_organizations_users_owner_id",
                        column: x => x.owner_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "organization_users",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    organization_id = table.Column<long>(type: "bigint", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    spaces_access_level = table.Column<byte>(type: "smallint", nullable: false),
                    epics_access_level = table.Column<byte>(type: "smallint", nullable: false),
                    issues_access_level = table.Column<byte>(type: "smallint", nullable: false),
                    admin_access_level = table.Column<byte>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_organization_users", x => x.id);
                    table.ForeignKey(
                        name: "fk_organization_users_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_organization_users_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

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
            
            migrationBuilder.Sql(@"
INSERT INTO organizations (name, owner_id, type, color, created_at, updated_at)
SELECT
    'Personal',
    u.id,
    1,
    '#3fb950',
    u.created_at,
    u.created_at
FROM users u;

update spaces s
set organization_id = o.id
from organizations o
where o.owner_id = s.creator_id;

insert into spaces (name, color, creator_id, created_at, updated_at, organization_id, is_default)
select 'Default Space', '#50b3b3', o.owner_id, o.created_at, o.created_at, o.id, true
from organizations o
where o.type = 1;

insert into epics (name, user_id, color, created_at, touched_at, updated_at, space_id, is_default)
select 'Backlog', s.creator_id, '#ff0000', s.created_at, s.created_at, s.created_at, s.id, true
from spaces s;

insert into statuses (name, epic_id, color, sort_order)
select 'New', e.id, '#dda61b', -1
from epics e 
where e.is_default = true;

insert into organization_users (user_id, organization_id, spaces_access_level, epics_access_level, issues_access_level, admin_access_level)
select o.owner_id, o.id, 15 /** All **/, 15 /** All **/, 15 /** All **/, 2 /** Update Org **/
from organizations o
where o.type = 1;

update epics e
set space_id = s.id
from spaces s
where e.space_id is null and s.is_default and s.creator_id = e.user_id;

update issues i
set space_id = s.id
from spaces s
where i.space_id is null and s.is_default = true and s.creator_id = i.user_id;

update issues i
set epic_id = e.id
from epics e
where i.epic_id is null and e.space_id = i.space_id and e.user_id = i.user_id;

update issues i
set status_id = (select id from statuses s where s.epic_id = i.epic_id order by s.sort_order limit 1)
where i.status_id is null;");

            migrationBuilder.AlterColumn<long>(
                name: "status_id",
                table: "issues",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "space_id",
                table: "epics",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);
            
            migrationBuilder.DropForeignKey(
                name: "fk_epics_spaces_space_id",
                table: "epics");

            migrationBuilder.DropForeignKey(
                name: "fk_issues_epics_epic_id",
                table: "issues");

            migrationBuilder.DropForeignKey(
                name: "fk_issues_spaces_space_id",
                table: "issues");

            migrationBuilder.DropForeignKey(
                name: "fk_issues_statuses_status_id",
                table: "issues");

            migrationBuilder.DropIndex(
                name: "ix_issues_epic_id",
                table: "issues");

            migrationBuilder.DropIndex(
                name: "ix_issues_space_id",
                table: "issues");

            migrationBuilder.DropColumn(
                name: "epic_id",
                table: "issues");

            migrationBuilder.DropColumn(
                name: "space_id",
                table: "issues");

            migrationBuilder.CreateIndex(
                name: "ix_spaces_organization_id",
                table: "spaces",
                column: "organization_id");

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

            migrationBuilder.CreateIndex(
                name: "ix_organization_users_organization_id",
                table: "organization_users",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ix_organization_users_user_id",
                table: "organization_users",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_organizations_owner_id_type",
                table: "organizations",
                columns: new[] { "owner_id", "type" },
                unique: true,
                filter: "type = 1");

            migrationBuilder.AddForeignKey(
                name: "fk_epics_spaces_space_id",
                table: "epics",
                column: "space_id",
                principalTable: "spaces",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_issues_statuses_status_id",
                table: "issues",
                column: "status_id",
                principalTable: "statuses",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_spaces_organizations_organization_id",
                table: "spaces",
                column: "organization_id",
                principalTable: "organizations",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_epics_spaces_space_id",
                table: "epics");

            migrationBuilder.DropForeignKey(
                name: "fk_issues_statuses_status_id",
                table: "issues");

            migrationBuilder.DropForeignKey(
                name: "fk_spaces_organizations_organization_id",
                table: "spaces");

            migrationBuilder.DropTable(
                name: "direct_epic_permissions");

            migrationBuilder.DropTable(
                name: "direct_space_permissions");

            migrationBuilder.DropTable(
                name: "organization_users");

            migrationBuilder.DropTable(
                name: "organizations");

            migrationBuilder.DropIndex(
                name: "ix_spaces_organization_id",
                table: "spaces");

            migrationBuilder.DropColumn(
                name: "color",
                table: "users");

            migrationBuilder.DropColumn(
                name: "is_default",
                table: "spaces");

            migrationBuilder.DropColumn(
                name: "organization_id",
                table: "spaces");

            migrationBuilder.DropColumn(
                name: "is_default",
                table: "epics");

            migrationBuilder.AlterColumn<string>(
                name: "color",
                table: "spaces",
                type: "character varying(7)",
                maxLength: 7,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(7)",
                oldMaxLength: 7);

            migrationBuilder.AlterColumn<long>(
                name: "status_id",
                table: "issues",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<long>(
                name: "epic_id",
                table: "issues",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "space_id",
                table: "issues",
                type: "bigint",
                nullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "space_id",
                table: "epics",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<string>(
                name: "color",
                table: "epics",
                type: "character varying(7)",
                maxLength: 7,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(7)",
                oldMaxLength: 7);

            migrationBuilder.CreateIndex(
                name: "ix_issues_epic_id",
                table: "issues",
                column: "epic_id");

            migrationBuilder.CreateIndex(
                name: "ix_issues_space_id",
                table: "issues",
                column: "space_id");

            migrationBuilder.AddForeignKey(
                name: "fk_epics_spaces_space_id",
                table: "epics",
                column: "space_id",
                principalTable: "spaces",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_issues_epics_epic_id",
                table: "issues",
                column: "epic_id",
                principalTable: "epics",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_issues_spaces_space_id",
                table: "issues",
                column: "space_id",
                principalTable: "spaces",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_issues_statuses_status_id",
                table: "issues",
                column: "status_id",
                principalTable: "statuses",
                principalColumn: "id");
        }
    }
}
