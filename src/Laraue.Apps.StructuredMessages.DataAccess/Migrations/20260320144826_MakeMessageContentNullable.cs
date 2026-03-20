using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Laraue.Apps.StructuredMessages.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class MakeMessageContentNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "content",
                table: "messages",
                type: "character varying(4096)",
                maxLength: 4096,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(4096)",
                oldMaxLength: 4096);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "content",
                table: "messages",
                type: "character varying(4096)",
                maxLength: 4096,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(4096)",
                oldMaxLength: 4096,
                oldNullable: true);
        }
    }
}
