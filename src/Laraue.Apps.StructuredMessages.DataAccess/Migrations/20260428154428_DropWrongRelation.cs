using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Laraue.Apps.StructuredMessages.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class DropWrongRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_issues_epics_epic_id",
                table: "issues");

            migrationBuilder.DropIndex(
                name: "ix_issues_epic_id",
                table: "issues");

            migrationBuilder.DropColumn(
                name: "epic_id",
                table: "issues");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "epic_id",
                table: "issues",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_issues_epic_id",
                table: "issues",
                column: "epic_id");

            migrationBuilder.AddForeignKey(
                name: "fk_issues_epics_epic_id",
                table: "issues",
                column: "epic_id",
                principalTable: "epics",
                principalColumn: "id");
        }
    }
}
