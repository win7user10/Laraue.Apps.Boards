using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Laraue.Apps.StructuredMessages.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndexToTelegramMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_telegram_messages_telegram_message_id_telegram_chat_id",
                table: "telegram_messages",
                columns: new[] { "telegram_message_id", "telegram_chat_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_telegram_messages_telegram_message_id_telegram_chat_id",
                table: "telegram_messages");
        }
    }
}
