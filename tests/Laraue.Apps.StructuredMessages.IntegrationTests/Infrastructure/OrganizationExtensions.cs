using Laraue.Apps.StructuredMessages.DataAccess.Models;

namespace Laraue.Apps.StructuredMessages.IntegrationTests.Infrastructure;

public static class OrganizationExtensions
{
    public static Issue GetIssue(
        this Organization organization,
        int spaceIndex,
        int epicIndex,
        int statusIndex,
        int issueIndex)
    {
        var space = organization.Spaces![spaceIndex];
        var epic = space.Epics![epicIndex];
        var defaultStatus = epic.Statuses![statusIndex];
        return defaultStatus.Issues![issueIndex];
    }
}