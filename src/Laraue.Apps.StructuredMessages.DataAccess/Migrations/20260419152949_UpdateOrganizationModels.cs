using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Laraue.Apps.StructuredMessages.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class UpdateOrganizationModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_issues_organization_organization_id",
                table: "issues");

            migrationBuilder.DropForeignKey(
                name: "fk_organization_users_owner_id",
                table: "organization");

            migrationBuilder.DropForeignKey(
                name: "fk_spaces_organization_organization_id",
                table: "spaces");

            migrationBuilder.DropPrimaryKey(
                name: "pk_organization",
                table: "organization");

            migrationBuilder.RenameTable(
                name: "organization",
                newName: "organizations");

            migrationBuilder.RenameIndex(
                name: "ix_organization_owner_id",
                table: "organizations",
                newName: "ix_organizations_owner_id");

            migrationBuilder.AddColumn<string>(
                name: "color",
                table: "organizations",
                type: "character varying(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "pk_organizations",
                table: "organizations",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_issues_organizations_organization_id",
                table: "issues",
                column: "organization_id",
                principalTable: "organizations",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_organizations_users_owner_id",
                table: "organizations",
                column: "owner_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_spaces_organizations_organization_id",
                table: "spaces",
                column: "organization_id",
                principalTable: "organizations",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_issues_organizations_organization_id",
                table: "issues");

            migrationBuilder.DropForeignKey(
                name: "fk_organizations_users_owner_id",
                table: "organizations");

            migrationBuilder.DropForeignKey(
                name: "fk_spaces_organizations_organization_id",
                table: "spaces");

            migrationBuilder.DropPrimaryKey(
                name: "pk_organizations",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "color",
                table: "organizations");

            migrationBuilder.RenameTable(
                name: "organizations",
                newName: "organization");

            migrationBuilder.RenameIndex(
                name: "ix_organizations_owner_id",
                table: "organization",
                newName: "ix_organization_owner_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_organization",
                table: "organization",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_issues_organization_organization_id",
                table: "issues",
                column: "organization_id",
                principalTable: "organization",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_organization_users_owner_id",
                table: "organization",
                column: "owner_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_spaces_organization_organization_id",
                table: "spaces",
                column: "organization_id",
                principalTable: "organization",
                principalColumn: "id");
        }
    }
}
