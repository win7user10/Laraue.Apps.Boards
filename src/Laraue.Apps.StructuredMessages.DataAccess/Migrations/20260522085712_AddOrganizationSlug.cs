using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Laraue.Apps.StructuredMessages.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationSlug : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "slug",
                table: "organizations",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");
            
            migrationBuilder.AddColumn<string>(
                name: "slug_postfix",
                table: "organizations",
                type: "character varying(4)",
                maxLength: 4,
                nullable: false,
                defaultValue: "");
            
            migrationBuilder.Sql(@"
UPDATE organizations o
SET slug = CASE
   WHEN o.type = 1 AND u.telegram_user_name IS NOT NULL THEN u.telegram_user_name
   WHEN o.type = 1 THEN 'user'
   ELSE 'organization'
END
FROM users u
WHERE o.owner_id = u.id;");

            migrationBuilder.Sql(@"
update organizations
set slug_postfix = (SELECT string_agg(
    substr('ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789',
           ceil(random() * 62)::int, 1),
    ''
) FROM generate_series(1, 4) WHERE id IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "ix_organizations_slug_postfix_slug",
                table: "organizations",
                columns: new[] { "slug_postfix", "slug" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_organizations_slug_postfix_slug",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "slug",
                table: "organizations");
            
            migrationBuilder.DropColumn(
                name: "slug_postfix",
                table: "organizations");
        }
    }
}
