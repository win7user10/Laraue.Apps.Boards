using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Laraue.Apps.StructuredMessages.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddAttributesIndicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "pk_issue_attribute_text_values",
                table: "issue_attribute_text_values");

            migrationBuilder.DropIndex(
                name: "ix_issue_attribute_text_values_issue_id",
                table: "issue_attribute_text_values");

            migrationBuilder.DropPrimaryKey(
                name: "pk_issue_attribute_list_values",
                table: "issue_attribute_list_values");

            migrationBuilder.DropIndex(
                name: "ix_issue_attribute_list_values_issue_id",
                table: "issue_attribute_list_values");

            migrationBuilder.AddPrimaryKey(
                name: "pk_issue_attribute_text_values",
                table: "issue_attribute_text_values",
                columns: new[] { "issue_id", "attribute_id" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_issue_attribute_list_values",
                table: "issue_attribute_list_values",
                columns: new[] { "issue_id", "attribute_id" });

            migrationBuilder.CreateIndex(
                name: "ix_issue_attribute_text_values_attribute_id",
                table: "issue_attribute_text_values",
                column: "attribute_id");

            migrationBuilder.CreateIndex(
                name: "ix_issue_attribute_text_values_text",
                table: "issue_attribute_text_values",
                column: "text")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_issue_attribute_list_values_attribute_id",
                table: "issue_attribute_list_values",
                column: "attribute_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "pk_issue_attribute_text_values",
                table: "issue_attribute_text_values");

            migrationBuilder.DropIndex(
                name: "ix_issue_attribute_text_values_attribute_id",
                table: "issue_attribute_text_values");

            migrationBuilder.DropIndex(
                name: "ix_issue_attribute_text_values_text",
                table: "issue_attribute_text_values");

            migrationBuilder.DropPrimaryKey(
                name: "pk_issue_attribute_list_values",
                table: "issue_attribute_list_values");

            migrationBuilder.DropIndex(
                name: "ix_issue_attribute_list_values_attribute_id",
                table: "issue_attribute_list_values");

            migrationBuilder.AddPrimaryKey(
                name: "pk_issue_attribute_text_values",
                table: "issue_attribute_text_values",
                columns: new[] { "attribute_id", "issue_id" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_issue_attribute_list_values",
                table: "issue_attribute_list_values",
                columns: new[] { "attribute_id", "issue_id" });

            migrationBuilder.CreateIndex(
                name: "ix_issue_attribute_text_values_issue_id",
                table: "issue_attribute_text_values",
                column: "issue_id");

            migrationBuilder.CreateIndex(
                name: "ix_issue_attribute_list_values_issue_id",
                table: "issue_attribute_list_values",
                column: "issue_id");
        }
    }
}
