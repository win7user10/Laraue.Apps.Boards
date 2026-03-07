using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Laraue.Apps.StructuredMessages.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AdditionalFieldsInMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_message_type_statuses_message_types_message_category_id",
                table: "message_type_statuses");

            migrationBuilder.DropForeignKey(
                name: "fk_message_types_users_user_id",
                table: "message_types");

            migrationBuilder.DropForeignKey(
                name: "fk_messages_message_type_statuses_message_type_status_id",
                table: "messages");

            migrationBuilder.DropForeignKey(
                name: "fk_messages_message_types_message_type_id",
                table: "messages");

            migrationBuilder.DropPrimaryKey(
                name: "pk_message_types",
                table: "message_types");

            migrationBuilder.DropPrimaryKey(
                name: "pk_message_type_statuses",
                table: "message_type_statuses");

            migrationBuilder.RenameTable(
                name: "message_types",
                newName: "message_categories");

            migrationBuilder.RenameTable(
                name: "message_type_statuses",
                newName: "message_statuses");

            migrationBuilder.RenameColumn(
                name: "message_type_status_id",
                table: "messages",
                newName: "status_id");

            migrationBuilder.RenameColumn(
                name: "message_type_id",
                table: "messages",
                newName: "category_id");

            migrationBuilder.RenameIndex(
                name: "ix_messages_message_type_status_id",
                table: "messages",
                newName: "ix_messages_status_id");

            migrationBuilder.RenameIndex(
                name: "ix_messages_message_type_id",
                table: "messages",
                newName: "ix_messages_category_id");

            migrationBuilder.RenameIndex(
                name: "ix_message_types_user_id",
                table: "message_categories",
                newName: "ix_message_categories_user_id");

            migrationBuilder.RenameIndex(
                name: "ix_message_type_statuses_message_category_id",
                table: "message_statuses",
                newName: "ix_message_statuses_message_category_id");

            migrationBuilder.AddColumn<string>(
                name: "sender",
                table: "messages",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "pk_message_categories",
                table: "message_categories",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_message_statuses",
                table: "message_statuses",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_message_categories_users_user_id",
                table: "message_categories",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_message_statuses_message_categories_message_category_id",
                table: "message_statuses",
                column: "message_category_id",
                principalTable: "message_categories",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_messages_message_categories_category_id",
                table: "messages",
                column: "category_id",
                principalTable: "message_categories",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_messages_message_statuses_status_id",
                table: "messages",
                column: "status_id",
                principalTable: "message_statuses",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_message_categories_users_user_id",
                table: "message_categories");

            migrationBuilder.DropForeignKey(
                name: "fk_message_statuses_message_categories_message_category_id",
                table: "message_statuses");

            migrationBuilder.DropForeignKey(
                name: "fk_messages_message_categories_category_id",
                table: "messages");

            migrationBuilder.DropForeignKey(
                name: "fk_messages_message_statuses_status_id",
                table: "messages");

            migrationBuilder.DropPrimaryKey(
                name: "pk_message_statuses",
                table: "message_statuses");

            migrationBuilder.DropPrimaryKey(
                name: "pk_message_categories",
                table: "message_categories");

            migrationBuilder.DropColumn(
                name: "sender",
                table: "messages");

            migrationBuilder.RenameTable(
                name: "message_statuses",
                newName: "message_type_statuses");

            migrationBuilder.RenameTable(
                name: "message_categories",
                newName: "message_types");

            migrationBuilder.RenameColumn(
                name: "status_id",
                table: "messages",
                newName: "message_type_status_id");

            migrationBuilder.RenameColumn(
                name: "category_id",
                table: "messages",
                newName: "message_type_id");

            migrationBuilder.RenameIndex(
                name: "ix_messages_status_id",
                table: "messages",
                newName: "ix_messages_message_type_status_id");

            migrationBuilder.RenameIndex(
                name: "ix_messages_category_id",
                table: "messages",
                newName: "ix_messages_message_type_id");

            migrationBuilder.RenameIndex(
                name: "ix_message_statuses_message_category_id",
                table: "message_type_statuses",
                newName: "ix_message_type_statuses_message_category_id");

            migrationBuilder.RenameIndex(
                name: "ix_message_categories_user_id",
                table: "message_types",
                newName: "ix_message_types_user_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_message_type_statuses",
                table: "message_type_statuses",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_message_types",
                table: "message_types",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_message_type_statuses_message_types_message_category_id",
                table: "message_type_statuses",
                column: "message_category_id",
                principalTable: "message_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_message_types_users_user_id",
                table: "message_types",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_messages_message_type_statuses_message_type_status_id",
                table: "messages",
                column: "message_type_status_id",
                principalTable: "message_type_statuses",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_messages_message_types_message_type_id",
                table: "messages",
                column: "message_type_id",
                principalTable: "message_types",
                principalColumn: "id");
        }
    }
}
