using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Laraue.Apps.StructuredMessages.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddFileTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "telegram_media_group_id",
                table: "messages",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "telegram_files",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    file_unique_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    size = table.Column<long>(type: "bigint", nullable: true),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_telegram_files", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "telegram_photos",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    message_id = table.Column<long>(type: "bigint", nullable: false),
                    width = table.Column<int>(type: "integer", nullable: false),
                    height = table.Column<int>(type: "integer", nullable: false),
                    telegram_file_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_telegram_photos", x => x.id);
                    table.ForeignKey(
                        name: "fk_telegram_photos_messages_message_id",
                        column: x => x.message_id,
                        principalTable: "messages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_telegram_photos_telegram_files_telegram_file_id",
                        column: x => x.telegram_file_id,
                        principalTable: "telegram_files",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_messages_telegram_media_group_id",
                table: "messages",
                column: "telegram_media_group_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_telegram_files_file_unique_id",
                table: "telegram_files",
                column: "file_unique_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_telegram_photos_message_id",
                table: "telegram_photos",
                column: "message_id");

            migrationBuilder.CreateIndex(
                name: "ix_telegram_photos_telegram_file_id",
                table: "telegram_photos",
                column: "telegram_file_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "telegram_photos");

            migrationBuilder.DropTable(
                name: "telegram_files");

            migrationBuilder.DropIndex(
                name: "ix_messages_telegram_media_group_id",
                table: "messages");

            migrationBuilder.DropColumn(
                name: "telegram_media_group_id",
                table: "messages");
        }
    }
}
