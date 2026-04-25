using Laraue.Apps.StructuredMessages.DataAccess;
using Laraue.Apps.StructuredMessages.DataAccess.Enums;
using Laraue.Apps.StructuredMessages.DataAccess.Models;
using Laraue.Apps.StructuredMessages.Services;

namespace Laraue.Apps.StructuredMessages.IntegrationTests.Infrastructure;

public class OrganizationInitializer(DatabaseContext context, Guid ownerId)
{
    private readonly Dictionary<Guid, Action<PermissionBuilder>> _organizationUsers = new()
    {
        [ownerId] = builder => builder.SetOrganizationAccessLevel(AccessLevel.All)
    };
    
    private string _organizationName = "TestOrganization";
    private string _organizationColor = "#ffffff";
    private DateTime _timestamp = DateTime.UtcNow;

    public OrganizationInitializer WithName(string name)
    {
        _organizationName = name;

        return this;
    }

    public OrganizationInitializer WithColor(string color)
    {
        _organizationColor = color;

        return this;
    }

    public OrganizationInitializer WithTimestamp(DateTime timestamp)
    {
        _timestamp = timestamp;

        return this;
    }

    public async Task<Organization> Initialize()
    {
        var organization = OrganizationDefaults.GetNewOrganizationEntity(
            ownerId,
            _organizationName,
            _organizationColor,
            _timestamp,
            OrganizationType.Organization);

        organization.Users = new List<OrganizationUser>();
        foreach (var user in _organizationUsers)
        {
            var builder = new PermissionBuilder();
            user.Value.Invoke(builder);

            var organizationUser = new OrganizationUser
            {
                AccessLevel = builder.OrganizationAccessLevel,
                UserId = user.Key
            };
            
            organization.Users.Add(organizationUser);
            
            // Set global space permissions
            context.Add(new SpaceOrganizationUser
            {
                AccessLevel = builder.SpaceAccessLevels.AccessLevel,
                OrganizationUser = organizationUser
            });
            
            // Set direct space permissions
            var spaceAccessLevels = builder.SpaceAccessLevels.DirectAccess;
            foreach (var accessLevel in spaceAccessLevels)
            {
                organization.Spaces![accessLevel.Key].Users = new List<SpaceOrganizationUser>
                {
                    new()
                    {
                        AccessLevel = accessLevel.Value,
                        OrganizationUser = organizationUser,
                    }
                };
            }
            
            // Set direct epic permissions
            var epicAccessLevelsBySpaceIndex = builder.EpicAccessLevels;
            foreach (var epicAccessLevelsBySpace in epicAccessLevelsBySpaceIndex)
            {
                foreach (var epicAccessLevels in epicAccessLevelsBySpace.Value.DirectAccess)
                {
                    organization.Spaces![epicAccessLevelsBySpace.Key].Epics![epicAccessLevels.Key].Users = new List<EpicOrganizationUser>
                    {
                        new()
                        {
                            AccessLevel = epicAccessLevels.Value,
                            OrganizationUser = organizationUser,
                        }
                    };
                }
            }
        }
        
        context.Add(organization);
        await context.SaveChangesAsync();
        
        return organization;
    }

    public OrganizationInitializer AddUser(Guid userId)
    {
        return AddUser(userId, _ => {});
    }

    public OrganizationInitializer AddUser(Guid userId, Action<PermissionBuilder> permissionBuilder)
    {
        _organizationUsers[userId] = permissionBuilder;

        return this;
    }

    public class PermissionBuilder
    {
        public AccessLevel OrganizationAccessLevel { get; private set; }
        public TestAccessLevels SpaceAccessLevels { get; private set; } = new();
        public Dictionary<int, TestAccessLevels> EpicAccessLevels { get; private set; } = new();
    
        public PermissionBuilder SetOrganizationAccessLevel(AccessLevel accessLevel)
        {
            OrganizationAccessLevel = accessLevel;
            return this;
        }
    
        public PermissionBuilder SetSpacesAccessLevel(AccessLevel accessLevel)
        {
            SpaceAccessLevels.AccessLevel = accessLevel;
            
            return this;
        }
    
        public PermissionBuilder SetDefaultSpaceBacklogAccessLevel(AccessLevel accessLevel)
        {
            EpicAccessLevels.TryAdd(0, new TestAccessLevels());
            EpicAccessLevels[0].DirectAccess[0] = accessLevel;
            
            return this;
        }
    }

    public class TestAccessLevels
    {
        public AccessLevel AccessLevel { get; set; }
        public Dictionary<int, AccessLevel> DirectAccess { get; set; } = new();
    }
}