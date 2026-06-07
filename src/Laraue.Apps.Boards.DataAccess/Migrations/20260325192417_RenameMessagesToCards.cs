using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Laraue.Apps.StructuredMessages.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RenameMessagesToCards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_telegram_photos_messages_message_id",
                table: "telegram_photos");

            migrationBuilder.DropForeignKey(
                name: "fk_telegram_videos_messages_message_id",
                table: "telegram_videos");

            migrationBuilder.RenameTable(
                name: "messages",
                newName: "cards");

            migrationBuilder.RenameTable(
                name: "message_statuses",
                newName: "card_statuses");

            migrationBuilder.RenameTable(
                name: "message_categories",
                newName: "card_categories");

            migrationBuilder.RenameColumn(
                name: "message_id",
                table: "telegram_photos",
                newName: "card_id");

            migrationBuilder.RenameIndex(
                name: "ix_telegram_photos_message_id",
                table: "telegram_photos",
                newName: "ix_telegram_photos_card_id");

            migrationBuilder.RenameIndex(
                name: "ix_message_categories_user_id",
                newName: "ix_card_categories_user_id",
                table: "card_categories");

            migrationBuilder.RenameIndex(
                name: "ix_message_statuses_message_category_id",
                newName: "ix_card_statuses_card_category_id",
                table: "card_statuses");

            migrationBuilder.RenameIndex(
                name: "ix_messages_category_id",
                newName: "ix_cards_category_id",
                table: "cards");
            
            migrationBuilder.RenameIndex(
                name: "ix_messages_content",
                newName: "ix_cards_content",
                table: "cards");
            
            migrationBuilder.RenameIndex(
                name: "ix_messages_status_id",
                newName: "ix_cards_status_id",
                table: "cards");

            migrationBuilder.RenameIndex(
                name: "ix_messages_telegram_media_group_id",
                newName: "ix_cards_telegram_media_group_id",
                table: "cards");

            migrationBuilder.RenameIndex(
                name: "ix_messages_user_id",
                newName: "ix_cards_user_id",
                table: "cards");

            migrationBuilder.AddForeignKey(
                name: "fk_telegram_photos_cards_card_id",
                table: "telegram_photos",
                column: "card_id",
                principalTable: "cards",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_telegram_videos_cards_message_id",
                table: "telegram_videos",
                column: "message_id",
                principalTable: "cards",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_telegram_photos_cards_card_id",
                table: "telegram_photos");

            migrationBuilder.DropForeignKey(
                name: "fk_telegram_videos_cards_message_id",
                table: "telegram_videos");

            migrationBuilder.RenameTable(
                newName: "messages",
                name: "cards");

            migrationBuilder.RenameTable(
                newName: "message_statuses",
                name: "card_statuses");

            migrationBuilder.RenameTable(
                newName: "message_categories",
                name: "card_categories");

            migrationBuilder.RenameColumn(
                name: "card_id",
                table: "telegram_photos",
                newName: "message_id");

            migrationBuilder.RenameIndex(
                name: "ix_telegram_photos_card_id",
                table: "telegram_photos",
                newName: "ix_telegram_photos_message_id");

            migrationBuilder.RenameIndex(
                newName: "ix_message_categories_user_id",
                name: "ix_card_categories_user_id",
                table: "card_categories");

            migrationBuilder.RenameIndex(
                newName: "ix_message_statuses_message_category_id",
                name: "ix_card_statuses_card_category_id",
                table: "card_statuses");

            migrationBuilder.RenameIndex(
                newName: "ix_messages_category_id",
                name: "ix_cards_category_id",
                table: "cards");
            
            migrationBuilder.RenameIndex(
                newName: "ix_messages_content",
                name: "ix_cards_content",
                table: "cards");
            
            migrationBuilder.RenameIndex(
                newName: "ix_messages_status_id",
                name: "ix_cards_status_id",
                table: "cards");

            migrationBuilder.RenameIndex(
                newName: "ix_messages_telegram_media_group_id",
                name: "ix_cards_telegram_media_group_id",
                table: "cards");

            migrationBuilder.RenameIndex(
                newName: "ix_messages_user_id",
                name: "ix_cards_user_id",
                table: "cards");

            migrationBuilder.AddForeignKey(
                name: "fk_telegram_photos_messages_message_id",
                table: "telegram_photos",
                column: "message_id",
                principalTable: "messages",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_telegram_videos_messages_message_id",
                table: "telegram_videos",
                column: "message_id",
                principalTable: "messages",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
