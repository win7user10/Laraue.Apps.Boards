using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Laraue.Apps.StructuredMessages.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddSortOrderToStatuses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_final",
                table: "message_statuses");

            migrationBuilder.AddColumn<int>(
                name: "sort_order",
                table: "message_statuses",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "sort_order",
                table: "message_statuses");

            migrationBuilder.AddColumn<bool>(
                name: "is_final",
                table: "message_statuses",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
