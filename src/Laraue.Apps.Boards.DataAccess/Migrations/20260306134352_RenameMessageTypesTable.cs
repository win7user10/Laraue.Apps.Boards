using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Laraue.Apps.StructuredMessages.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RenameMessageTypesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_message_type_statuses_message_types_message_type_id",
                table: "message_type_statuses");

            migrationBuilder.RenameColumn(
                name: "message_type_id",
                table: "message_type_statuses",
                newName: "message_category_id");

            migrationBuilder.RenameIndex(
                name: "ix_message_type_statuses_message_type_id",
                table: "message_type_statuses",
                newName: "ix_message_type_statuses_message_category_id");

            migrationBuilder.AddForeignKey(
                name: "fk_message_type_statuses_message_types_message_category_id",
                table: "message_type_statuses",
                column: "message_category_id",
                principalTable: "message_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_message_type_statuses_message_types_message_category_id",
                table: "message_type_statuses");

            migrationBuilder.RenameColumn(
                name: "message_category_id",
                table: "message_type_statuses",
                newName: "message_type_id");

            migrationBuilder.RenameIndex(
                name: "ix_message_type_statuses_message_category_id",
                table: "message_type_statuses",
                newName: "ix_message_type_statuses_message_type_id");

            migrationBuilder.AddForeignKey(
                name: "fk_message_type_statuses_message_types_message_type_id",
                table: "message_type_statuses",
                column: "message_type_id",
                principalTable: "message_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
