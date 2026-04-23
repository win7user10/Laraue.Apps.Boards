using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Laraue.Apps.StructuredMessages.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationIdToEpic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "organization_id",
                table: "epics",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_epics_organization_id",
                table: "epics",
                column: "organization_id");

            migrationBuilder.AddForeignKey(
                name: "fk_epics_organizations_organization_id",
                table: "epics",
                column: "organization_id",
                principalTable: "organizations",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_epics_organizations_organization_id",
                table: "epics");

            migrationBuilder.DropIndex(
                name: "ix_epics_organization_id",
                table: "epics");

            migrationBuilder.DropColumn(
                name: "organization_id",
                table: "epics");
        }
    }
}
