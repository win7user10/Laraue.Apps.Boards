using Laraue.Apps.StructuredMessages.DataAccess.Enums;
using Laraue.Apps.StructuredMessages.IntegrationTests.Infrastructure;
using Laraue.Apps.StructuredMessages.WebApiHost.Controllers;
using Laraue.Apps.StructuredMessages.WebApiServices;
using Laraue.Core.Exceptions.Web;
using LinqToDB.EntityFrameworkCore;

namespace Laraue.Apps.StructuredMessages.IntegrationTests;

[Collection("IntegrationTest")]
public class IssuesControllerTests(WebApiTestHost host)  : IClassFixture<WebApiTestHost>
{
    private readonly Proxy<IssuesController> _issuesController = host.Controller<IssuesController>();
    
    [Fact]
    public async Task User_ShouldCreateIssue_WhenIsOrganizationOwner()
    {
        using var testScope = host.CreateTestScope();
        var userId = await testScope.CreateUser();
        var organization = await testScope.InitializeOrganization(userId);

        var status = organization.GetStatus(0, 0, 0);
        
        var issueId = await _issuesController
            .WithOrganizationAuthorization(organization.Id, userId)
            .Execute(x => x.Create(
                new CreateIssueRequest
                {
                    Content = "New Issue",
                    StatusId = status.Id
                }));

        var issue = await testScope.Database.Issues.FirstAsyncEF(e => e.Id == issueId);
        Assert.Equal("New Issue", issue.Content);
    }
    
    [Fact]
    public async Task User_ShouldNotCreateIssue_WhenHasNoAccess()
    {
        using var testScope = host.CreateTestScope();
        var userId = await testScope.CreateUser();
        var participatorId = await testScope.CreateUser();
        var organization = await testScope.InitializeOrganization(
            userId,
            organization => organization.AddUser(participatorId));

        var status = organization.GetStatus(0, 0, 0);

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => _issuesController
            .WithOrganizationAuthorization(organization.Id, participatorId)
            .Execute(x => x.Create(
                new CreateIssueRequest
                {
                    Content = "New Issue",
                    StatusId = status.Id
                })));
        
        var notFound = ex.HasInnerException<NotFoundException>();
        Assert.Equal($"Status: {status.Id} is not found, or Create permission is missing for Epic contains this status", notFound.Message);
    }
    
    [Fact]
    public async Task User_ShouldCreateIssue_WhenHasGlobalAccessToCreateIssues()
    {
        using var testScope = host.CreateTestScope();
        var userId = await testScope.CreateUser();
        var participatorId = await testScope.CreateUser();
        var organization = await testScope.InitializeOrganization(
            userId,
            organization => organization
                .AddUser(participatorId, u => u
                    .SetIssuesAccessLevel(ChildrenAccessLevel.Create)));

        var status = organization.GetStatus(0, 0, 0);
        
        var issueId = await _issuesController
            .WithOrganizationAuthorization(organization.Id, userId)
            .Execute(x => x.Create(
                new CreateIssueRequest
                {
                    Content = "New Issue",
                    StatusId = status.Id
                }));

        var issue = await testScope.Database.Issues.FirstAsyncEF(e => e.Id == issueId);
        Assert.Equal("New Issue", issue.Content);
    }
}