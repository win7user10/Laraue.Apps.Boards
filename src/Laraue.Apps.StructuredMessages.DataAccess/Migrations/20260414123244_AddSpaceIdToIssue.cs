using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Laraue.Apps.StructuredMessages.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddSpaceIdToIssue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "space_id",
                table: "issues",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_issues_space_id",
                table: "issues",
                column: "space_id");

            migrationBuilder.AddForeignKey(
                name: "fk_issues_spaces_space_id",
                table: "issues",
                column: "space_id",
                principalTable: "spaces",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_issues_spaces_space_id",
                table: "issues");

            migrationBuilder.DropIndex(
                name: "ix_issues_space_id",
                table: "issues");

            migrationBuilder.DropColumn(
                name: "space_id",
                table: "issues");
        }
    }
}
