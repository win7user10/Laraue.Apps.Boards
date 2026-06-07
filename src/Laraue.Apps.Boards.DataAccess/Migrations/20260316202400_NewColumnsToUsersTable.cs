using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Laraue.Apps.StructuredMessages.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class NewColumnsToUsersTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "sender",
                table: "messages");

            migrationBuilder.AddColumn<string>(
                name: "telegram_first_name",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "telegram_last_name",
                table: "users",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "telegram_first_name",
                table: "users");

            migrationBuilder.DropColumn(
                name: "telegram_last_name",
                table: "users");

            migrationBuilder.AddColumn<string>(
                name: "sender",
                table: "messages",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);
        }
    }
}
