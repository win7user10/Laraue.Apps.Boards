using Laraue.Apps.StructuredMessages.DataAccess.Models;

namespace Laraue.Apps.StructuredMessages.Services;

public record OrganizationAuthData
{
    public long OrganizationId { get; init; }
    public Guid UserId { get; init; }
    public OrganizationType OrganizationType => IdService.NullId == OrganizationId
        ? OrganizationType.Personal
        : OrganizationType.Organization;
}