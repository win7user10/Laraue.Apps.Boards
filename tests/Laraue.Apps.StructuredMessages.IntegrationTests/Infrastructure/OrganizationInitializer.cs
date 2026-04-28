using Laraue.Apps.StructuredMessages.DataAccess;
using Laraue.Apps.StructuredMessages.DataAccess.Enums;
using Laraue.Apps.StructuredMessages.DataAccess.Models;
using Laraue.Apps.StructuredMessages.Services;
using Status = Laraue.Apps.StructuredMessages.DataAccess.Models.Status;

namespace Laraue.Apps.StructuredMessages.IntegrationTests.Infrastructure;

public class OrganizationInitializer(DatabaseContext context, Guid ownerId)
{
    private readonly Dictionary<Guid, Action<PermissionBuilder>> _organizationUsers = new();
    
    private string _organizationName = "TestOrganization";
    private string _organizationColor = "#ffffff";
    private DateTime _timestamp = DateTime.UtcNow;
    private OrganizationType _type = OrganizationType.Organization;

    private List<SpaceBuilder> _spaces = new ();

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

    public OrganizationInitializer WithType(OrganizationType type)
    {
        _type = type;

        return this;
    }

    public async Task<Organization> Initialize()
    {
        var organization = OrganizationDefaults.GetNewOrganizationEntity(
            ownerId,
            _organizationName,
            _organizationColor,
            _timestamp,
            _type);

        foreach (var space in _spaces)
        {
            var spaceEntity = new Space
            {
                Name = space.SpaceName,
                Color = space.SpaceColor,
                CreatedAt = space.Timestamp,
                UpdatedAt = space.Timestamp,
                CreatorId = space.CreatorId,
                Epics = new List<Epic>
                {
                    OrganizationDefaults.GetNewBacklogEpicEntity(space.CreatorId, space.Timestamp)
                }
            };

            foreach (var epic in space.Epics)
            {
                var epicEntity = new Epic
                {
                    Name = epic.EpicName,
                    Color = epic.EpicColor,
                    CreatedAt = epic.Timestamp,
                    UpdatedAt = epic.Timestamp,
                    TouchedAt = epic.Timestamp,
                    UserId = epic.CreatorId,
                    Statuses = new List<Status>
                    {
                        OrganizationDefaults.GetNewStatusEntity(),
                    }
                };

                foreach (var status in epic.Statuses)
                {
                    var statusEntity = new Status
                    {
                        Name = status.StatusName
                    };
                    
                    epicEntity.Statuses.Add(statusEntity);
                }

                foreach (var issuesByStatusIndex in epic.Issues)
                {
                    var statusForIssue = epicEntity.Statuses[issuesByStatusIndex.Key];
                    statusForIssue.Issues ??= new List<Issue>();
                    foreach (var issue in issuesByStatusIndex.Value)
                    {
                        statusForIssue.Issues.Add(new Issue
                        {
                            Content = issue.Content,
                            UserId = issue.CreatorId,
                        });
                    }
                }
                
                spaceEntity.Epics.Add(epicEntity);
            }
            
            organization.Spaces!.Add(spaceEntity);
        }

        foreach (var user in _organizationUsers)
        {
            var builder = new PermissionBuilder();
            user.Value.Invoke(builder);

            var organizationUser = new OrganizationUser
            {
                ItemAccessLevel = builder.OrganizationItemAccessLevel,
                AdminAccessLevel = builder.OrganizationAdminAccessLevel,
                UserId = user.Key
            };
            
            organization.Users!.Add(organizationUser);
            
            // Set global space permissions
            context.Add(new SpaceOrganizationUser
            {
                ItemAccessLevel = builder.SpacesGlobalAccessLevel,
                OrganizationUser = organizationUser
            });

            // Set global epic permissions
            context.Add(new EpicOrganizationUser
            {
                ItemAccessLevel = builder.EpicsGlobalAccessLevel,
                OrganizationUser = organizationUser
            });
            
            // Set direct space permissions
            foreach (var accessLevel in builder.SpaceAccessLevels)
            {
                organization.Spaces![accessLevel.Key].Users = new List<SpaceOrganizationUser>
                {
                    new()
                    {
                        ItemAccessLevel = accessLevel.Value,
                        OrganizationUser = organizationUser,
                    }
                };
            }
            
            // Set direct epic permissions
            var epicAccessLevelsBySpaceIndex = builder.EpicAccessLevels;
            foreach (var epicAccessLevelsBySpace in epicAccessLevelsBySpaceIndex)
            {
                foreach (var epicAccessLevels in epicAccessLevelsBySpace.Value)
                {
                    organization.Spaces![epicAccessLevelsBySpace.Key].Epics![epicAccessLevels.Key].Users = new List<EpicOrganizationUser>
                    {
                        new()
                        {
                            ItemAccessLevel = epicAccessLevels.Value,
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

    public OrganizationInitializer AddSpace(Guid creatorId)
    {
        return AddSpace(creatorId, _ => {});
    }

    public OrganizationInitializer AddSpace(Guid creatorId, Action<SpaceBuilder> spaceBuilder)
    {
        var builder = new SpaceBuilder(creatorId, _timestamp);
        spaceBuilder(builder);
        
        _spaces.Add(builder);

        return this;
    }

    public class PermissionBuilder
    {
        public ItemAccessLevel OrganizationItemAccessLevel { get; private set; }
        public AdminAccessLevel OrganizationAdminAccessLevel { get; private set; }
        public ItemAccessLevel SpacesGlobalAccessLevel { get; private set; } = new();
        public DirectAccess SpaceAccessLevels { get; private set; } = new();
        public ItemAccessLevel EpicsGlobalAccessLevel { get; private set; } = new();
        public Dictionary<int, DirectAccess> EpicAccessLevels { get; private set; } = new();
    
        public PermissionBuilder SetOrganizationAccessLevel(ItemAccessLevel itemAccessLevel)
        {
            OrganizationItemAccessLevel = itemAccessLevel;
            return this;
        }
        
        public PermissionBuilder SetOrganizationAccessLevel(AdminAccessLevel adminAccessLevel)
        {
            OrganizationAdminAccessLevel = adminAccessLevel;
            return this;
        }
    
        public PermissionBuilder SetSpacesAccessLevel(ItemAccessLevel itemAccessLevel)
        {
            SpacesGlobalAccessLevel = itemAccessLevel;
            
            return this;
        }
    
        public PermissionBuilder SetSpaceAccessLevel(int index, ItemAccessLevel itemAccessLevel)
        {
            SpaceAccessLevels[index] = itemAccessLevel;
            
            return this;
        }
    
        public PermissionBuilder SetDefaultSpaceBacklogAccessLevel(ItemAccessLevel itemAccessLevel)
        {
            EpicAccessLevels.TryAdd(0, new DirectAccess());
            EpicAccessLevels[0][0] = itemAccessLevel;
            
            return this;
        }
    
        public PermissionBuilder SetEpicsAccessLevel(ItemAccessLevel itemAccessLevel)
        {
            EpicsGlobalAccessLevel = itemAccessLevel;
            
            return this;
        }
    }

    public class SpaceBuilder(Guid creatorId, DateTime timestamp)
    {
        public string SpaceName { get; private set; } = "AdditionalSpace";
        public string SpaceColor { get; private set; }  = "#ffffff";
        public DateTime Timestamp { get; private set; }  = timestamp;
        public Guid CreatorId { get; private set; }  = creatorId;
        public List<EpicBuilder> Epics { get;} = new();

        public SpaceBuilder WithName(string name)
        {
            SpaceName = name;

            return this;
        }
        
        public SpaceBuilder WithColor(string color)
        {
            SpaceColor = color;

            return this;
        }
        
        public SpaceBuilder WithTimestamp(DateTime timestamp)
        {
            Timestamp = timestamp;

            return this;
        }

        public SpaceBuilder AddEpic(Guid creatorId)
        {
            return AddEpic(creatorId, _ => {});
        }

        public SpaceBuilder AddEpic(Guid creatorId, Action<EpicBuilder> epicBuilder)
        {
            var builder = new EpicBuilder(creatorId, Timestamp);
            epicBuilder(builder);
            Epics.Add(builder);

            return this;
        }
    }
    
    public class EpicBuilder(Guid creatorId, DateTime timestamp)
    {
        public string EpicName { get; private set; } = "AdditionalEpic";
        public string EpicColor { get; private set; }  = "#121212";
        public DateTime Timestamp { get; private set; }  = timestamp;
        public Guid CreatorId { get; private set; }  = creatorId;
        public List<StatusBuilder> Statuses  { get; private set; } = new ();
        public Dictionary<int, List<IssueBuilder>> Issues  { get; private set; } = new ();
        
        public EpicBuilder WithName(string name)
        {
            EpicName = name;

            return this;
        }
        
        public EpicBuilder WithColor(string color)
        {
            EpicColor = color;

            return this;
        }
        
        public EpicBuilder WithTimestamp(DateTime value)
        {
            Timestamp = value;

            return this;
        }
        
        public EpicBuilder AddStatus(Action<StatusBuilder> statusBuilder)
        {
            var builder = new StatusBuilder();
            statusBuilder(builder);
            
            Statuses.Add(builder);

            return this;
        }

        public EpicBuilder AddIssue(Guid creatorId, int statusIndex)
        {
            return AddIssue(creatorId, statusIndex, _ => { });
        }

        public EpicBuilder AddIssue(Guid creatorId, int statusIndex, Action<IssueBuilder> issueBuilder)
        {
            var builder = new IssueBuilder(creatorId);
            issueBuilder(builder);

            if (!Issues.ContainsKey(statusIndex))
                Issues[statusIndex] = [];
            Issues[statusIndex].Add(builder);

            return this;
        }
    }

    public class StatusBuilder
    {
        public string StatusName { get; private set; } = "AdditionalStatus";
        
        public StatusBuilder WithName(string name)
        {
            StatusName = name;

            return this;
        }
    }

    public class IssueBuilder(Guid creatorId)
    {
        public Guid CreatorId { get; } = creatorId;
        public string Content { get; private set; } = "IssueContent";
        
        public IssueBuilder WithContent(string name)
        {
            Content = name;

            return this;
        }
    }

    public class DirectAccess : Dictionary<int, ItemAccessLevel>
    {
    }
}