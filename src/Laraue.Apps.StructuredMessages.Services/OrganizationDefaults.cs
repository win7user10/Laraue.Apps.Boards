using Laraue.Apps.StructuredMessages.DataAccess.Enums;
using Laraue.Apps.StructuredMessages.DataAccess.Models;

namespace Laraue.Apps.StructuredMessages.Services;

public class OrganizationDefaults
{
    public static Organization GetNewOrganizationEntity(
        Guid userId,
        string slug,
        string organizationName,
        string organizationColor,
        DateTime timestamp,
        bool isPersonal)
    {
        var organizationUser = new OrganizationUser
        {
            EpicsAccessLevel = ChildrenAccessLevel.All,
            SpacesAccessLevel = ChildrenAccessLevel.All,
            IssuesAccessLevel = ChildrenAccessLevel.All,
            AdminAccessLevel = isPersonal
                ? AdminAccessLevel.UpdateOrganization | AdminAccessLevel.MassMove
                : AdminAccessLevel.All,
            UserId = userId,
        };
        
        return new Organization
        {
            OwnerId = userId,
            Name = organizationName,
            Color = organizationColor,
            CreatedAt = timestamp,
            UpdatedAt = timestamp,
            Type = isPersonal ? OrganizationType.Personal : OrganizationType.Organization,
            JoinCode = isPersonal ? null : StringGenerator.GenerateJoinCode(),
            SlugPostfix = StringGenerator.GenerateOrganizationPostfix(),
            Slug = slug,
            Users = new List<OrganizationUser> { organizationUser },
            Spaces = new List<Space>
            {
                new()
                {
                    Name = "Default Space",
                    Color = Palette.RandomColor(),
                    CreatedAt = timestamp,
                    UpdatedAt = timestamp,
                    IsDefault = true,
                    CreatorId = userId,
                    Epics = new List<Epic> { GetNewBacklogEpicEntity(userId, timestamp) },
                }
            }
        };
    }

    public static Epic GetNewBacklogEpicEntity(
        Guid userId,
        DateTime timestamp)
    {
        return new()
        {
            Name = "Backlog",
            Color = Palette.DefaultBacklogColor,
            CreatedAt = timestamp,
            TouchedAt = timestamp,
            UpdatedAt = timestamp,
            UserId = userId,
            IsDefault = true,
            Statuses = new List<DataAccess.Models.Status>()
            {
                GetNewStatusEntity(),
            }
        };
    }

    public static DataAccess.Models.Status GetNewStatusEntity()
    {
        return new()
        {
            Name = "New",
            Color = Palette.RandomColor(),
            SortOrder = 0,
        };
    }

    public static string GetPersonalOrganizationSlug(string? telegramUserName)
    {
        return telegramUserName ?? "user";
    }
    
    public static string GetPersonalOrganizationName(string? languageCode)
    {
        return languageCode == "ru" ? "Personal" : "Личное"; // TODO - move to lang files
    }
}