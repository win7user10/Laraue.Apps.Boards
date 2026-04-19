using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Laraue.Apps.StructuredMessages.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class CreateOrganizationTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "organization_id",
                table: "spaces",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "organization_id",
                table: "issues",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "organization",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_organization", x => x.id);
                    table.ForeignKey(
                        name: "fk_organization_users_owner_id",
                        column: x => x.owner_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_spaces_organization_id",
                table: "spaces",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ix_issues_organization_id",
                table: "issues",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ix_organization_owner_id",
                table: "organization",
                column: "owner_id");

            migrationBuilder.AddForeignKey(
                name: "fk_issues_organization_organization_id",
                table: "issues",
                column: "organization_id",
                principalTable: "organization",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_spaces_organization_organization_id",
                table: "spaces",
                column: "organization_id",
                principalTable: "organization",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_issues_organization_organization_id",
                table: "issues");

            migrationBuilder.DropForeignKey(
                name: "fk_spaces_organization_organization_id",
                table: "spaces");

            migrationBuilder.DropTable(
                name: "organization");

            migrationBuilder.DropIndex(
                name: "ix_spaces_organization_id",
                table: "spaces");

            migrationBuilder.DropIndex(
                name: "ix_issues_organization_id",
                table: "issues");

            migrationBuilder.DropColumn(
                name: "organization_id",
                table: "spaces");

            migrationBuilder.DropColumn(
                name: "organization_id",
                table: "issues");
        }
    }
}
