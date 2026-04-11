using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Laraue.Apps.StructuredMessages.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RenameEntitiesFks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_issues_card_categories_category_id",
                table: "issues");

            migrationBuilder.DropForeignKey(
                name: "fk_issues_card_statuses_status_id",
                table: "issues");

            migrationBuilder.DropForeignKey(
                name: "fk_statuses_epics_card_category_id",
                table: "statuses");

            migrationBuilder.RenameColumn(
                name: "card_category_id",
                table: "statuses",
                newName: "epic_id");

            migrationBuilder.RenameIndex(
                name: "ix_statuses_card_category_id",
                table: "statuses",
                newName: "ix_statuses_epic_id");

            migrationBuilder.RenameColumn(
                name: "category_id",
                table: "issues",
                newName: "epic_id");

            migrationBuilder.RenameIndex(
                name: "ix_issues_category_id",
                table: "issues",
                newName: "ix_issues_epic_id");

            migrationBuilder.AddForeignKey(
                name: "fk_issues_epics_epic_id",
                table: "issues",
                column: "epic_id",
                principalTable: "epics",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_issues_statuses_status_id",
                table: "issues",
                column: "status_id",
                principalTable: "statuses",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_statuses_epics_epic_id",
                table: "statuses",
                column: "epic_id",
                principalTable: "epics",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_issues_epics_epic_id",
                table: "issues");

            migrationBuilder.DropForeignKey(
                name: "fk_issues_statuses_status_id",
                table: "issues");

            migrationBuilder.DropForeignKey(
                name: "fk_statuses_epics_epic_id",
                table: "statuses");

            migrationBuilder.RenameColumn(
                name: "epic_id",
                table: "statuses",
                newName: "card_category_id");

            migrationBuilder.RenameIndex(
                name: "ix_statuses_epic_id",
                table: "statuses",
                newName: "ix_statuses_card_category_id");

            migrationBuilder.RenameColumn(
                name: "epic_id",
                table: "issues",
                newName: "category_id");

            migrationBuilder.RenameIndex(
                name: "ix_issues_epic_id",
                table: "issues",
                newName: "ix_issues_category_id");

            migrationBuilder.AddForeignKey(
                name: "fk_issues_card_categories_category_id",
                table: "issues",
                column: "category_id",
                principalTable: "epics",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_issues_card_statuses_status_id",
                table: "issues",
                column: "status_id",
                principalTable: "statuses",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_statuses_epics_card_category_id",
                table: "statuses",
                column: "card_category_id",
                principalTable: "epics",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
