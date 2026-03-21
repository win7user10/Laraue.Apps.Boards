using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Laraue.Apps.StructuredMessages.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddVideosTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "telegram_videos",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    message_id = table.Column<long>(type: "bigint", nullable: false),
                    width = table.Column<int>(type: "integer", nullable: false),
                    height = table.Column<int>(type: "integer", nullable: false),
                    file_id = table.Column<Guid>(type: "uuid", nullable: false),
                    thumbnail_width = table.Column<int>(type: "integer", nullable: true),
                    thumbnail_height = table.Column<int>(type: "integer", nullable: true),
                    thumbnail_file_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_telegram_videos", x => x.id);
                    table.ForeignKey(
                        name: "fk_telegram_videos_messages_message_id",
                        column: x => x.message_id,
                        principalTable: "messages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_telegram_videos_telegram_files_file_id",
                        column: x => x.file_id,
                        principalTable: "telegram_files",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_telegram_videos_telegram_files_thumbnail_file_id",
                        column: x => x.thumbnail_file_id,
                        principalTable: "telegram_files",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_telegram_videos_file_id",
                table: "telegram_videos",
                column: "file_id");

            migrationBuilder.CreateIndex(
                name: "ix_telegram_videos_message_id",
                table: "telegram_videos",
                column: "message_id");

            migrationBuilder.CreateIndex(
                name: "ix_telegram_videos_thumbnail_file_id",
                table: "telegram_videos",
                column: "thumbnail_file_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "telegram_videos");
        }
    }
}
