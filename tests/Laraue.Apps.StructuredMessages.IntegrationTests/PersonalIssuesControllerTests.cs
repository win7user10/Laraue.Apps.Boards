using Laraue.Apps.StructuredMessages.IntegrationTests.Infrastructure;
using Laraue.Apps.StructuredMessages.WebApiHost.Controllers;
using Laraue.Apps.StructuredMessages.WebApiServices;
using LinqToDB.EntityFrameworkCore;

namespace Laraue.Apps.StructuredMessages.IntegrationTests;

[Collection("IntegrationTest")]
public class PersonalIssuesControllerTests(WebApiTestHost host)  : IClassFixture<WebApiTestHost>
{
    private readonly Proxy<IssuesController> _issuesController = host.Controller<IssuesController>();
    
    [Fact]
    public async Task User_ShouldCreatePersonalIssue_Always()
    {
        using var testScope = host.CreateTestScope();
        var userId = await testScope.CreateUser();
        var organization = await testScope.InitializePersonalOrganization(userId);

        var space = organization.Spaces![0];
        var backlog = space.Epics![0];
        var defaultStatus = backlog.Statuses![0];
        
        var issueId = await _issuesController
            .WithOrganizationAuthorization(organization.Id, userId)
            .Execute(x => x.Create(
                new CreateIssueRequest
                {
                    Content = "New Issue",
                    StatusId = defaultStatus.Id
                }));

        var issue = await testScope.Database.Issues.FirstAsyncEF(e => e.Id == issueId);
        
        Assert.Equal("New Issue", issue.Content);
        Assert.Equal(defaultStatus.Id, issue.StatusId);
        Assert.NotEqual(default, issue.CreatedAt);
        Assert.NotEqual(default, issue.UpdatedAt);
        Assert.Equal(userId, issue.UserId);
    }
    
    [Fact]
    public async Task User_ShouldUpdatePersonalIssue_Always()
    {
        using var testScope = host.CreateTestScope();
        var userId = await testScope.CreateUser();
        var organization = await testScope.InitializePersonalOrganization(
            userId,
            o => o
                .AddSpace(userId, s => s
                    .AddEpic(userId, e => e
                        .AddIssue(userId, 0, i => i
                            .WithContent("Hi")))));

        var issue = organization.GetIssue(1, 1, 0, 0);
        
        await _issuesController
            .WithOrganizationAuthorization(organization.Id, userId)
            .Execute(x => x.Update(
                issue.Id,
                new UpdateIssueRequest
                {
                    Content = "New",
                }));

        issue = await testScope.Database.Issues.FirstAsyncEF(e => e.Id == issue.Id);
        
        Assert.True(issue.CreatedAt < issue.UpdatedAt);
        Assert.Equal("New", issue.Content);
    }
    
    [Fact]
    public async Task User_ShouldDeletePersonalIssue_Always()
    {
        using var testScope = host.CreateTestScope();
        var userId = await testScope.CreateUser();
        var organization = await testScope.InitializePersonalOrganization(
            userId,
            o => o
                .AddSpace(userId, s => s
                    .AddEpic(userId, e => e
                        .AddIssue(userId, 0))));

        var issue = organization.GetIssue(1, 1, 0, 0);
        
        await _issuesController
            .WithOrganizationAuthorization(organization.Id, userId)
            .Execute(x => x.Delete(issue.Id));

        issue = await testScope.Database.Issues.FirstOrDefaultAsyncEF(e => e.Id == issue.Id);
        Assert.Null(issue);
    }
}