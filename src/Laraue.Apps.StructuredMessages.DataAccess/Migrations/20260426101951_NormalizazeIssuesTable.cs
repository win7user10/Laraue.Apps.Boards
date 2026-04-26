using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Laraue.Apps.StructuredMessages.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class NormalizazeIssuesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_issues_epics_epic_id",
                table: "issues");

            migrationBuilder.DropForeignKey(
                name: "fk_issues_spaces_space_id",
                table: "issues");

            migrationBuilder.DropIndex(
                name: "ix_issues_space_id",
                table: "issues");

            migrationBuilder.DropColumn(
                name: "space_id",
                table: "issues");

            migrationBuilder.AlterColumn<long>(
                name: "epic_id",
                table: "issues",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddForeignKey(
                name: "fk_issues_epics_epic_id",
                table: "issues",
                column: "epic_id",
                principalTable: "epics",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_issues_epics_epic_id",
                table: "issues");

            migrationBuilder.AlterColumn<long>(
                name: "epic_id",
                table: "issues",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AddColumn<long>(
                name: "space_id",
                table: "issues",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "ix_issues_space_id",
                table: "issues",
                column: "space_id");

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
        }
    }
}
