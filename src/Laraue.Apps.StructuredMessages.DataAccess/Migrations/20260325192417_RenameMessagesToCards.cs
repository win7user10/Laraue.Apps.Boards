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

            migrationBuilder.DropTable(
                name: "cards");

            migrationBuilder.DropTable(
                name: "card_statuses");

            migrationBuilder.DropTable(
                name: "card_categories");

            migrationBuilder.RenameColumn(
                name: "card_id",
                table: "telegram_photos",
                newName: "message_id");

            migrationBuilder.RenameIndex(
                name: "ix_telegram_photos_card_id",
                table: "telegram_photos",
                newName: "ix_telegram_photos_message_id");

            migrationBuilder.CreateTable(
                name: "message_categories",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_message_categories", x => x.id);
                    table.ForeignKey(
                        name: "fk_message_categories_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "message_statuses",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    message_category_id = table.Column<long>(type: "bigint", nullable: false),
                    color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_message_statuses", x => x.id);
                    table.ForeignKey(
                        name: "fk_message_statuses_message_categories_message_category_id",
                        column: x => x.message_category_id,
                        principalTable: "message_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "messages",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    category_id = table.Column<long>(type: "bigint", nullable: true),
                    status_id = table.Column<long>(type: "bigint", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    content = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    telegram_media_group_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    telegram_message_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_messages", x => x.id);
                    table.ForeignKey(
                        name: "fk_messages_message_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "message_categories",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_messages_message_statuses_status_id",
                        column: x => x.status_id,
                        principalTable: "message_statuses",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_messages_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_message_categories_user_id",
                table: "message_categories",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_message_statuses_message_category_id",
                table: "message_statuses",
                column: "message_category_id");

            migrationBuilder.CreateIndex(
                name: "ix_messages_category_id",
                table: "messages",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_messages_content",
                table: "messages",
                column: "content")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_messages_status_id",
                table: "messages",
                column: "status_id");

            migrationBuilder.CreateIndex(
                name: "ix_messages_telegram_media_group_id",
                table: "messages",
                column: "telegram_media_group_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_messages_user_id",
                table: "messages",
                column: "user_id");

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
