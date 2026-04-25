using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Laraue.Apps.StructuredMessages.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class PersonalOrganizationUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_organizations_owner_id",
                table: "organizations");

            migrationBuilder.CreateIndex(
                name: "ix_organizations_owner_id_type",
                table: "organizations",
                columns: new[] { "owner_id", "type" },
                unique: true,
                filter: "type = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_organizations_owner_id_type",
                table: "organizations");

            migrationBuilder.CreateIndex(
                name: "ix_organizations_owner_id",
                table: "organizations",
                column: "owner_id");
        }
    }
}
