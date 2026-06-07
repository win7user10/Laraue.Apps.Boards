using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Laraue.Apps.StructuredMessages.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RenameTelegramMessagesColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "telegram_message_id",
                table: "telegram_messages",
                newName: "external_message_id");

            migrationBuilder.RenameColumn(
                name: "telegram_chat_id",
                table: "telegram_messages",
                newName: "external_chat_id");

            migrationBuilder.RenameIndex(
                name: "ix_telegram_messages_telegram_message_id_telegram_chat_id",
                table: "telegram_messages",
                newName: "ix_telegram_messages_external_message_id_external_chat_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "external_message_id",
                table: "telegram_messages",
                newName: "telegram_message_id");

            migrationBuilder.RenameColumn(
                name: "external_chat_id",
                table: "telegram_messages",
                newName: "telegram_chat_id");

            migrationBuilder.RenameIndex(
                name: "ix_telegram_messages_external_message_id_external_chat_id",
                table: "telegram_messages",
                newName: "ix_telegram_messages_telegram_message_id_telegram_chat_id");
        }
    }
}
