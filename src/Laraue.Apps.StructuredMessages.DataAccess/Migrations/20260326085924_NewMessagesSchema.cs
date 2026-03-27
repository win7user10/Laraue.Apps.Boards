using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Laraue.Apps.StructuredMessages.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class NewMessagesSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "telegram_media_groups",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    external_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_telegram_media_groups", x => x.id);
                });
            
            migrationBuilder.Sql(@"
insert into telegram_media_groups
(external_id)
select distinct telegram_media_group_id
from cards c
where telegram_media_group_id is not null");
            
            migrationBuilder.CreateTable(
                name: "telegram_messages",
                columns: table => new
                {
                    id = table.Column<long>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    telegram_message_id = table.Column<int>(type: "integer", nullable: true),
                    telegram_media_group_id = table.Column<long>(type: "bigint", nullable: true),
                    telegram_chat_id = table.Column<long>(type: "bigint", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_telegram_messages", x => x.id);
                    table.ForeignKey(
                        name: "fk_telegram_messages_telegram_media_groups_telegram_media_grou",
                        column: x => x.telegram_media_group_id,
                        principalTable: "telegram_media_groups",
                        principalColumn: "id");
                });
            
            migrationBuilder.Sql(@"
insert into telegram_messages
(telegram_message_id, telegram_media_group_id, telegram_chat_id)
select c.telegram_message_id, tmg.id, u.telegram_id
from cards c
left join telegram_media_groups tmg on tmg.external_id = c.telegram_media_group_id
inner join users u on u.id = c.user_id
where c.telegram_message_id is not null");
            
            
            migrationBuilder.Sql(@"
WITH updated_data AS (
    SELECT c.id as card_id, tm.id as message_id
    FROM cards c
    INNER JOIN users u ON u.id = c.user_id
    INNER JOIN telegram_messages tm ON tm.telegram_message_id = c.telegram_message_id
    WHERE tm.telegram_chat_id = u.telegram_id
)
UPDATE cards c
SET telegram_message_id = ud.message_id
FROM updated_data ud
WHERE c.id = ud.card_id;");
            
            migrationBuilder.AlterColumn<long>(
                name: "telegram_message_id",
                table: "cards",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
            
            migrationBuilder.DropForeignKey(
                name: "fk_telegram_photos_cards_card_id",
                table: "telegram_photos");

            migrationBuilder.DropForeignKey(
                name: "fk_telegram_videos_cards_message_id",
                table: "telegram_videos");

            migrationBuilder.DropIndex(
                name: "ix_telegram_videos_message_id",
                table: "telegram_videos");

            migrationBuilder.DropIndex(
                name: "ix_telegram_photos_card_id",
                table: "telegram_photos");

            migrationBuilder.DropIndex(
                name: "ix_cards_telegram_media_group_id",
                table: "cards");
            
            migrationBuilder.AddColumn<long>(
                name: "telegram_message_id",
                table: "telegram_videos",
                type: "integer",
                nullable: false,
                defaultValue: 0);
            
            migrationBuilder.Sql(@"
update telegram_videos tv
set telegram_message_id = c.telegram_message_id
from cards c
where tv.message_id = c.id");

            migrationBuilder.DropColumn(
                name: "message_id",
                table: "telegram_videos");

            migrationBuilder.AddColumn<long>(
                name: "telegram_message_id",
                table: "telegram_photos",
                type: "integer",
                nullable: false,
                defaultValue: 0);
            
            migrationBuilder.Sql(@"
update telegram_photos tp
set telegram_message_id = c.telegram_message_id
from cards c
where tp.card_id = c.id");

            migrationBuilder.DropColumn(
                name: "card_id",
                table: "telegram_photos");

            migrationBuilder.DropColumn(
                name: "telegram_media_group_id",
                table: "cards");

            migrationBuilder.CreateIndex(
                name: "ix_telegram_videos_telegram_message_id",
                table: "telegram_videos",
                column: "telegram_message_id");

            migrationBuilder.CreateIndex(
                name: "ix_telegram_photos_telegram_message_id",
                table: "telegram_photos",
                column: "telegram_message_id");

            migrationBuilder.CreateIndex(
                name: "ix_cards_telegram_message_id",
                table: "cards",
                column: "telegram_message_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_telegram_media_groups_external_id",
                table: "telegram_media_groups",
                column: "external_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_telegram_messages_telegram_media_group_id",
                table: "telegram_messages",
                column: "telegram_media_group_id");

            migrationBuilder.AddForeignKey(
                name: "fk_cards_telegram_messages_telegram_message_id",
                table: "cards",
                column: "telegram_message_id",
                principalTable: "telegram_messages",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_telegram_photos_telegram_messages_telegram_message_id",
                table: "telegram_photos",
                column: "telegram_message_id",
                principalTable: "telegram_messages",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_telegram_videos_telegram_messages_telegram_message_id",
                table: "telegram_videos",
                column: "telegram_message_id",
                principalTable: "telegram_messages",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_cards_telegram_messages_telegram_message_id",
                table: "cards");

            migrationBuilder.DropForeignKey(
                name: "fk_telegram_photos_telegram_messages_telegram_message_id",
                table: "telegram_photos");

            migrationBuilder.DropForeignKey(
                name: "fk_telegram_videos_telegram_messages_telegram_message_id",
                table: "telegram_videos");

            migrationBuilder.DropTable(
                name: "telegram_messages");

            migrationBuilder.DropTable(
                name: "telegram_media_groups");

            migrationBuilder.DropIndex(
                name: "ix_telegram_videos_telegram_message_id",
                table: "telegram_videos");

            migrationBuilder.DropIndex(
                name: "ix_telegram_photos_telegram_message_id",
                table: "telegram_photos");

            migrationBuilder.DropIndex(
                name: "ix_cards_telegram_message_id",
                table: "cards");

            migrationBuilder.DropColumn(
                name: "telegram_message_id",
                table: "telegram_videos");

            migrationBuilder.DropColumn(
                name: "telegram_message_id",
                table: "telegram_photos");

            migrationBuilder.AddColumn<long>(
                name: "message_id",
                table: "telegram_videos",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "card_id",
                table: "telegram_photos",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "telegram_media_group_id",
                table: "cards",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_telegram_videos_message_id",
                table: "telegram_videos",
                column: "message_id");

            migrationBuilder.CreateIndex(
                name: "ix_telegram_photos_card_id",
                table: "telegram_photos",
                column: "card_id");

            migrationBuilder.CreateIndex(
                name: "ix_cards_telegram_media_group_id",
                table: "cards",
                column: "telegram_media_group_id",
                unique: true);

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
    }
}
