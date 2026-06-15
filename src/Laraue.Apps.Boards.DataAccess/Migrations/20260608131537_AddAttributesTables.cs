using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Laraue.Apps.StructuredMessages.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddAttributesTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "attributes",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    organization_id = table.Column<long>(type: "bigint", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    attribute_type = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_attributes", x => x.id);
                    table.ForeignKey(
                        name: "fk_attributes_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "attribute_list_values",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    attribute_id = table.Column<long>(type: "bigint", nullable: false),
                    value = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_attribute_list_values", x => x.id);
                    table.ForeignKey(
                        name: "fk_attribute_list_values_attributes_attribute_id",
                        column: x => x.attribute_id,
                        principalTable: "attributes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "issue_attribute_text_values",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    issue_id = table.Column<long>(type: "bigint", nullable: false),
                    attribute_id = table.Column<long>(type: "bigint", nullable: false),
                    text = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_issue_attribute_text_values", x => x.id);
                    table.ForeignKey(
                        name: "fk_issue_attribute_text_values_attributes_attribute_id",
                        column: x => x.attribute_id,
                        principalTable: "attributes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_issue_attribute_text_values_issues_issue_id",
                        column: x => x.issue_id,
                        principalTable: "issues",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "issue_attribute_list_values",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    issue_id = table.Column<long>(type: "bigint", nullable: false),
                    attribute_id = table.Column<long>(type: "bigint", nullable: false),
                    attribute_list_value_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_issue_attribute_list_values", x => x.id);
                    table.ForeignKey(
                        name: "fk_issue_attribute_list_values_attribute_list_values_attribute",
                        column: x => x.attribute_list_value_id,
                        principalTable: "attribute_list_values",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_issue_attribute_list_values_attributes_attribute_id",
                        column: x => x.attribute_id,
                        principalTable: "attributes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_issue_attribute_list_values_issues_issue_id",
                        column: x => x.issue_id,
                        principalTable: "issues",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(@"
update organization_users
set admin_access_level = admin_access_level | 16
where exists (select 1 from organizations where owner_id = user_id)");

            migrationBuilder.CreateIndex(
                name: "ix_attribute_list_values_attribute_id",
                table: "attribute_list_values",
                column: "attribute_id");

            migrationBuilder.CreateIndex(
                name: "ix_attributes_organization_id",
                table: "attributes",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ix_issue_attribute_list_values_attribute_id",
                table: "issue_attribute_list_values",
                column: "attribute_id");

            migrationBuilder.CreateIndex(
                name: "ix_issue_attribute_list_values_attribute_list_value_id",
                table: "issue_attribute_list_values",
                column: "attribute_list_value_id");

            migrationBuilder.CreateIndex(
                name: "ix_issue_attribute_list_values_issue_id",
                table: "issue_attribute_list_values",
                column: "issue_id");

            migrationBuilder.CreateIndex(
                name: "ix_issue_attribute_text_values_attribute_id",
                table: "issue_attribute_text_values",
                column: "attribute_id");

            migrationBuilder.CreateIndex(
                name: "ix_issue_attribute_text_values_issue_id",
                table: "issue_attribute_text_values",
                column: "issue_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "issue_attribute_list_values");

            migrationBuilder.DropTable(
                name: "issue_attribute_text_values");

            migrationBuilder.DropTable(
                name: "attribute_list_values");

            migrationBuilder.DropTable(
                name: "attributes");
        }
    }
}
