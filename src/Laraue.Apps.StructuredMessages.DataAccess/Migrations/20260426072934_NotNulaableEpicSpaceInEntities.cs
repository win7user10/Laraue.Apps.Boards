using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Laraue.Apps.StructuredMessages.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class NotNulaableEpicSpaceInEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
insert into organizations (name, owner_id, type, color, created_at, updated_at)
select 'Personal', u.id, 1, '#3fb950', u.created_at, u.created_at
from users u;

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

insert into organization_users (user_id, organization_id, access_level)
select o.owner_id, o.id, 1
from organizations o
where o.type = 1;

insert into space_organization_users (space_id, organization_user_id, access_level)
select s.id, ou.id, 4
from spaces s
         inner join organization_users ou on ou.organization_id = s.organization_id
where s.is_default;

update epics e
set space_id = s.id
from spaces s
where e.space_id is null and s.is_default and s.creator_id = e.user_id;

update issues i
set space_id = s.id,
    organization_id = s.organization_id
from spaces s
where i.space_id is null and s.is_default = true and s.creator_id = i.user_id;

update issues i
set epic_id = e.id
from epics e where i.epic_id is null and e.space_id = i.space_id and e.user_id = i.user_id;

update issues i
set status_id = (select id from statuses s where s.epic_id = i.epic_id order by s.sort_order limit 1)
where i.status_id is null;");
            
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

            migrationBuilder.DropForeignKey(
                name: "fk_spaces_organizations_organization_id",
                table: "spaces");

            migrationBuilder.AlterColumn<long>(
                name: "organization_id",
                table: "spaces",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

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
                table: "issues",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "epic_id",
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

            migrationBuilder.AddForeignKey(
                name: "fk_epics_spaces_space_id",
                table: "epics",
                column: "space_id",
                principalTable: "spaces",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_issues_epics_epic_id",
                table: "issues",
                column: "epic_id",
                principalTable: "epics",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_issues_spaces_space_id",
                table: "issues",
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
                name: "fk_issues_epics_epic_id",
                table: "issues");

            migrationBuilder.DropForeignKey(
                name: "fk_issues_spaces_space_id",
                table: "issues");

            migrationBuilder.DropForeignKey(
                name: "fk_issues_statuses_status_id",
                table: "issues");

            migrationBuilder.DropForeignKey(
                name: "fk_spaces_organizations_organization_id",
                table: "spaces");

            migrationBuilder.AlterColumn<long>(
                name: "organization_id",
                table: "spaces",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<long>(
                name: "status_id",
                table: "issues",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<long>(
                name: "space_id",
                table: "issues",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<long>(
                name: "epic_id",
                table: "issues",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<long>(
                name: "space_id",
                table: "epics",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

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

            migrationBuilder.AddForeignKey(
                name: "fk_spaces_organizations_organization_id",
                table: "spaces",
                column: "organization_id",
                principalTable: "organizations",
                principalColumn: "id");
        }
    }
}
