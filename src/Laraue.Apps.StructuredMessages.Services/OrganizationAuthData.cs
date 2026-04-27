namespace Laraue.Apps.StructuredMessages.Services;

public record OrganizationAuthData
{
    public long OrganizationId { get; init; }
    public Guid UserId { get; init; }
}