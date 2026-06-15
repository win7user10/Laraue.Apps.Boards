using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Laraue.Apps.StructuredMessages.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddAttributesTables2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "pk_issue_attribute_text_values",
                table: "issue_attribute_text_values");

            migrationBuilder.DropIndex(
                name: "ix_issue_attribute_text_values_attribute_id",
                table: "issue_attribute_text_values");

            migrationBuilder.DropPrimaryKey(
                name: "pk_issue_attribute_list_values",
                table: "issue_attribute_list_values");

            migrationBuilder.DropIndex(
                name: "ix_issue_attribute_list_values_attribute_id",
                table: "issue_attribute_list_values");

            migrationBuilder.DropColumn(
                name: "id",
                table: "issue_attribute_text_values");

            migrationBuilder.AlterColumn<long>(
                name: "id",
                table: "issue_attribute_list_values",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddPrimaryKey(
                name: "pk_issue_attribute_text_values",
                table: "issue_attribute_text_values",
                columns: new[] { "attribute_id", "issue_id" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_issue_attribute_list_values",
                table: "issue_attribute_list_values",
                columns: new[] { "attribute_id", "issue_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "pk_issue_attribute_text_values",
                table: "issue_attribute_text_values");

            migrationBuilder.DropPrimaryKey(
                name: "pk_issue_attribute_list_values",
                table: "issue_attribute_list_values");

            migrationBuilder.AddColumn<long>(
                name: "id",
                table: "issue_attribute_text_values",
                type: "bigint",
                nullable: false,
                defaultValue: 0L)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<long>(
                name: "id",
                table: "issue_attribute_list_values",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddPrimaryKey(
                name: "pk_issue_attribute_text_values",
                table: "issue_attribute_text_values",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_issue_attribute_list_values",
                table: "issue_attribute_list_values",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "ix_issue_attribute_text_values_attribute_id",
                table: "issue_attribute_text_values",
                column: "attribute_id");

            migrationBuilder.CreateIndex(
                name: "ix_issue_attribute_list_values_attribute_id",
                table: "issue_attribute_list_values",
                column: "attribute_id");
        }
    }
}
