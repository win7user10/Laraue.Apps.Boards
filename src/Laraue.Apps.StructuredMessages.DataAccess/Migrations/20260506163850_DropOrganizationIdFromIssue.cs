using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Laraue.Apps.StructuredMessages.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class DropOrganizationIdFromIssue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_issues_organizations_organization_id",
                table: "issues");

            migrationBuilder.DropIndex(
                name: "ix_issues_organization_id",
                table: "issues");

            migrationBuilder.DropColumn(
                name: "organization_id",
                table: "issues");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "organization_id",
                table: "issues",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_issues_organization_id",
                table: "issues",
                column: "organization_id");

            migrationBuilder.AddForeignKey(
                name: "fk_issues_organizations_organization_id",
                table: "issues",
                column: "organization_id",
                principalTable: "organizations",
                principalColumn: "id");
        }
    }
}
