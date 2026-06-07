using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Laraue.Apps.StructuredMessages.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RenameMainEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_message_categories_users_user_id",
                table: "card_categories");

            migrationBuilder.DropForeignKey(
                name: "fk_message_statuses_message_categories_message_category_id",
                table: "card_statuses");

            migrationBuilder.DropForeignKey(
                name: "fk_messages_message_categories_category_id",
                table: "cards");

            migrationBuilder.DropForeignKey(
                name: "fk_messages_message_statuses_status_id",
                table: "cards");

            migrationBuilder.DropForeignKey(
                name: "fk_cards_telegram_messages_telegram_message_id",
                table: "cards");

            migrationBuilder.DropForeignKey(
                name: "fk_messages_users_user_id",
                table: "cards");

            migrationBuilder.DropPrimaryKey(
                name: "pk_messages",
                table: "cards");

            migrationBuilder.DropPrimaryKey(
                name: "pk_message_statuses",
                table: "card_statuses");

            migrationBuilder.DropPrimaryKey(
                name: "pk_message_categories",
                table: "card_categories");

            migrationBuilder.RenameTable(
                name: "cards",
                newName: "issues");

            migrationBuilder.RenameTable(
                name: "card_statuses",
                newName: "statuses");

            migrationBuilder.RenameTable(
                name: "card_categories",
                newName: "epics");

            migrationBuilder.RenameIndex(
                name: "ix_cards_user_id",
                table: "issues",
                newName: "ix_issues_user_id");

            migrationBuilder.RenameIndex(
                name: "ix_cards_telegram_message_id",
                table: "issues",
                newName: "ix_issues_telegram_message_id");

            migrationBuilder.RenameIndex(
                name: "ix_cards_status_id",
                table: "issues",
                newName: "ix_issues_status_id");

            migrationBuilder.RenameIndex(
                name: "ix_cards_content",
                table: "issues",
                newName: "ix_issues_content");

            migrationBuilder.RenameIndex(
                name: "ix_cards_category_id",
                table: "issues",
                newName: "ix_issues_category_id");

            migrationBuilder.RenameIndex(
                name: "ix_card_statuses_card_category_id",
                table: "statuses",
                newName: "ix_statuses_card_category_id");

            migrationBuilder.RenameIndex(
                name: "ix_card_categories_user_id",
                table: "epics",
                newName: "ix_epics_user_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_issues",
                table: "issues",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_statuses",
                table: "statuses",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_epics",
                table: "epics",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_epics_users_user_id",
                table: "epics",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

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
                name: "fk_issues_telegram_messages_telegram_message_id",
                table: "issues",
                column: "telegram_message_id",
                principalTable: "telegram_messages",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_issues_users_user_id",
                table: "issues",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_statuses_epics_card_category_id",
                table: "statuses",
                column: "card_category_id",
                principalTable: "epics",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_epics_users_user_id",
                table: "epics");

            migrationBuilder.DropForeignKey(
                name: "fk_issues_card_categories_category_id",
                table: "issues");

            migrationBuilder.DropForeignKey(
                name: "fk_issues_card_statuses_status_id",
                table: "issues");

            migrationBuilder.DropForeignKey(
                name: "fk_issues_telegram_messages_telegram_message_id",
                table: "issues");

            migrationBuilder.DropForeignKey(
                name: "fk_issues_users_user_id",
                table: "issues");

            migrationBuilder.DropForeignKey(
                name: "fk_statuses_epics_card_category_id",
                table: "statuses");

            migrationBuilder.DropPrimaryKey(
                name: "pk_statuses",
                table: "statuses");

            migrationBuilder.DropPrimaryKey(
                name: "pk_issues",
                table: "issues");

            migrationBuilder.DropPrimaryKey(
                name: "pk_epics",
                table: "epics");

            migrationBuilder.RenameTable(
                name: "statuses",
                newName: "card_statuses");

            migrationBuilder.RenameTable(
                name: "issues",
                newName: "cards");

            migrationBuilder.RenameTable(
                name: "epics",
                newName: "card_categories");

            migrationBuilder.RenameIndex(
                name: "ix_statuses_card_category_id",
                table: "card_statuses",
                newName: "ix_card_statuses_card_category_id");

            migrationBuilder.RenameIndex(
                name: "ix_issues_user_id",
                table: "cards",
                newName: "ix_cards_user_id");

            migrationBuilder.RenameIndex(
                name: "ix_issues_telegram_message_id",
                table: "cards",
                newName: "ix_cards_telegram_message_id");

            migrationBuilder.RenameIndex(
                name: "ix_issues_status_id",
                table: "cards",
                newName: "ix_cards_status_id");

            migrationBuilder.RenameIndex(
                name: "ix_issues_content",
                table: "cards",
                newName: "ix_cards_content");

            migrationBuilder.RenameIndex(
                name: "ix_issues_category_id",
                table: "cards",
                newName: "ix_cards_category_id");

            migrationBuilder.RenameIndex(
                name: "ix_epics_user_id",
                table: "card_categories",
                newName: "ix_card_categories_user_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_card_statuses",
                table: "card_statuses",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_cards",
                table: "cards",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_card_categories",
                table: "card_categories",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_card_categories_users_user_id",
                table: "card_categories",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_card_statuses_card_categories_card_category_id",
                table: "card_statuses",
                column: "card_category_id",
                principalTable: "card_categories",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_cards_card_categories_category_id",
                table: "cards",
                column: "category_id",
                principalTable: "card_categories",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_cards_card_statuses_status_id",
                table: "cards",
                column: "status_id",
                principalTable: "card_statuses",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_cards_telegram_messages_telegram_message_id",
                table: "cards",
                column: "telegram_message_id",
                principalTable: "telegram_messages",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_cards_users_user_id",
                table: "cards",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
