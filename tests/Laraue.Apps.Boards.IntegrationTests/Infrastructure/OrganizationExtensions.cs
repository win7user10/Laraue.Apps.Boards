using Laraue.Apps.Boards.DataAccess.Models;
using Attribute = Laraue.Apps.Boards.DataAccess.Models.Attribute;

namespace Laraue.Apps.Boards.IntegrationTests.Infrastructure;

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
        var epic = organization.GetEpic(spaceIndex, epicIndex);
        return epic.Statuses![statusIndex];
    }
    
    public static Epic GetEpic(
        this Organization organization,
        int spaceIndex,
        int epicIndex)
    {
        var space = organization.GetSpace(spaceIndex);
        return space.Epics![epicIndex];
    }
    
    public static Space GetSpace(
        this Organization organization,
        int spaceIndex)
    {
        return organization.Spaces![spaceIndex];
    }
    
    public static Attribute GetAttribute(
        this Organization organization,
        int attributeIndex)
    {
        return organization.Attributes![attributeIndex];
    }
    
    public static AttributeListValue GetListValue(
        this Attribute attribute,
        int valueIndex)
    {
        return attribute.AttributeListValues![valueIndex];
    }
}