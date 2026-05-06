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
        var status = organization.GetStatus(spaceIndex, epicIndex, statusIndex);
        return status.Issues![issueIndex];
    }
    
    public static Status GetStatus(
        this Organization organization,
        int spaceIndex,
        int epicIndex,
        int statusIndex)
    {
        var space = organization.Spaces![spaceIndex];
        var epic = space.Epics![epicIndex];
        return epic.Statuses![statusIndex];
    }
}