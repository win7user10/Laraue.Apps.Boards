using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Laraue.Apps.StructuredMessages.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class InsertDefaultEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
insert into organizations (name, owner_id, type, color, created_at, updated_at)
select 'Personal', u.id, 1, '#3fb950', u.created_at, u.created_at 
from users u;

insert into spaces (name, color, creator_id, created_at, updated_at, organization_id, is_default)
select 'Default Space', '#50b3b3', o.owner_id, o.created_at, o.created_at, o.id, true
from organizations o 
where o.type = 1;

insert into epics (name, user_id, color, created_at, touched_at, updated_at, organization_id, space_id, is_default)
select 'Backlog', s.creator_id, '#ff0000', s.created_at, s.created_at, s.created_at, s.organization_id, s.id, true
from spaces s;

insert into organization_users (user_id, organization_id, access_level)
select o.owner_id, o.id, 1
from organizations o
where o.type = 1;

insert into space_organization_users (space_id, organization_user_id, access_level)
select s.id, ou.id, 4
from spaces s
inner join organization_users ou on ou.organization_id = s.organization_id
where s.is_default;

update epics e
set space_id = e.space_id
from spaces s
where e.space_id is null and s.is_default and s.creator_id = e.user_id;

update issues i
set space_id = s.id,
    organization_id = s.organization_id
from spaces s
where i.space_id is null and s.is_default = true and s.creator_id = i.user_id;

update issues i
set epic_id = e.id
from epics e
where i.epic_id is null and e.space_id = i.space_id and e.user_id = i.user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
