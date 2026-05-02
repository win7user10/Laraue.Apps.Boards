using Laraue.Apps.StructuredMessages.DataAccess;
using Laraue.Apps.StructuredMessages.DataAccess.Enums;
using Laraue.Core.DataAccess.EFCore.Extensions;

namespace Laraue.Apps.StructuredMessages.Services;

public interface IAccessService
{
    Task<ItemAccessLevels> GetChildrenAccessLevels(
        OrganizationAuthData authData,
        CancellationToken cancellationToken);
}

public class AccessService(DatabaseContext context) : IAccessService
{
    public Task<ItemAccessLevels> GetChildrenAccessLevels(
        OrganizationAuthData authData,
        CancellationToken cancellationToken)
    {
        return context.OrganizationUsers
            .Where(o => o.OrganizationId == authData.OrganizationId)
            .Where(o => o.UserId == authData.UserId)
            .Select(o => new ItemAccessLevels
            {
                EpicsAccessLevel = o.EpicsAccessLevel,
                SpacesAccessLevel = o.SpacesAccessLevel,
                IssuesAccessLevel = o.IssuesAccessLevel,
            })
            .FirstOrThrowNotFoundEFAsync("User is not exists or has been removed from organization", cancellationToken);
    }
}

public record ItemAccessLevels
{
    public ChildrenAccessLevel SpacesAccessLevel { get; set; }
    public ChildrenAccessLevel EpicsAccessLevel { get; set; }
    public ChildrenAccessLevel IssuesAccessLevel { get; set; }
}