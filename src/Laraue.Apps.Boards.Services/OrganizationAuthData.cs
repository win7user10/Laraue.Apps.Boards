namespace Laraue.Apps.Boards.Services;

public record OrganizationAuthData
{
    public long OrganizationId { get; init; }
    public Guid UserId { get; init; }
}