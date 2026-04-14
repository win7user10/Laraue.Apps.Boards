using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Laraue.Apps.StructuredMessages.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddSpaceIdToEpic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "space_id",
                table: "epics",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_epics_space_id",
                table: "epics",
                column: "space_id");

            migrationBuilder.AddForeignKey(
                name: "fk_epics_spaces_space_id",
                table: "epics",
                column: "space_id",
                principalTable: "spaces",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_epics_spaces_space_id",
                table: "epics");

            migrationBuilder.DropIndex(
                name: "ix_epics_space_id",
                table: "epics");

            migrationBuilder.DropColumn(
                name: "space_id",
                table: "epics");
        }
    }
}
