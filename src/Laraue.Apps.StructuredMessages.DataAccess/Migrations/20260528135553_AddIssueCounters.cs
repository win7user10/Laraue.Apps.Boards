using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Laraue.Apps.StructuredMessages.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddIssueCounters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "issue_numbers",
                columns: table => new
                {
                    issue_id = table.Column<long>(type: "bigint", nullable: false),
                    space_id = table.Column<long>(type: "bigint", nullable: false),
                    number = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_issue_numbers", x => x.issue_id);
                    table.ForeignKey(
                        name: "fk_issue_numbers_issues_issue_id",
                        column: x => x.issue_id,
                        principalTable: "issues",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_issue_numbers_spaces_space_id",
                        column: x => x.space_id,
                        principalTable: "spaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });
            
            
            // Insert issue numbers
            migrationBuilder.Sql(@"
WITH issue_counters as (
    select i.id, e.space_id, row_number() over (partition by e.space_id) as number from issues i
    inner join statuses s on i.status_id = s.id
    inner join epics e on s.epic_id = e.id
    order by i.id
)

insert into issue_numbers
select ic.id, ic.space_id, ic.number
from issue_counters ic;");
            
            migrationBuilder.CreateTable(
                name: "space_counters",
                columns: table => new
                {
                    space_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    last_number = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_space_counters", x => x.space_id);
                });
            
            // Set space counters
            migrationBuilder.Sql(@"
WITH issue_counters as (
    select id, (
        select ic.number from issues i
        inner join statuses st on i.status_id = st.id
        inner join epics e on st.epic_id = e.id and e.space_id = s.id
        inner join issue_numbers ic on ic.issue_id = i.id
        limit 1) as number
    from spaces s
)

insert into space_counters
select id, number
from issue_counters
where number is not null");

            migrationBuilder.CreateIndex(
                name: "ix_issue_numbers_space_id_number",
                table: "issue_numbers",
                columns: new[] { "space_id", "number" },
                unique: true);
            
            migrationBuilder.AddColumn<string>(
                name: "key",
                table: "spaces",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(@"
update spaces
set key = upper(substr(name, 0, 4))");
            
            migrationBuilder.DropIndex(
                name: "ix_spaces_organization_id",
                table: "spaces");

            migrationBuilder.CreateIndex(
                name: "ix_spaces_organization_id_key",
                table: "spaces",
                columns: new[] { "organization_id", "key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "space_counters");
            
            migrationBuilder.DropColumn(
                name: "key",
                table: "spaces");
            
            migrationBuilder.DropIndex(
                name: "ix_spaces_organization_id_key",
                table: "spaces");

            migrationBuilder.CreateIndex(
                name: "ix_spaces_organization_id",
                table: "spaces",
                column: "organization_id");
            
            migrationBuilder.DropTable(
                name: "issue_numbers");
        }
    }
}
