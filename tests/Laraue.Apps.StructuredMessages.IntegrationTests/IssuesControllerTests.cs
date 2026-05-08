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
            .WithOrganizationAuthorization(organization.Id, participatorId)
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
    public async Task User_ShouldNotCreateIssue_WhenHasGlobalAccessOnlyToCreateEpics()
    {
        using var testScope = host.CreateTestScope();
        var userId = await testScope.CreateUser();
        var participatorId = await testScope.CreateUser();
        var organization = await testScope.InitializeOrganization(
            userId,
            organization => organization
                .AddUser(participatorId, u => u
                    .SetEpicsAccessLevel(ChildrenAccessLevel.Create)));

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
    public async Task User_ShouldCreateIssue_WhenHasIssuesAccessOnSpaceLevel()
    {
        using var testScope = host.CreateTestScope();
        var userId = await testScope.CreateUser();
        var participatorId = await testScope.CreateUser();
        var organization = await testScope.InitializeOrganization(
            userId,
            organization => organization
                .AddUser(participatorId, u => u
                    .SetSpaceIssuesAccessLevel(0, ChildrenAccessLevel.Create)));

        var status = organization.GetStatus(0, 0, 0);
        
        var issueId = await _issuesController
            .WithOrganizationAuthorization(organization.Id, participatorId)
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
    public async Task User_ShouldNotCreateIssue_WhenHasIssuesAccessOnOtherSpaceLevel()
    {
        using var testScope = host.CreateTestScope();
        var userId = await testScope.CreateUser();
        var participatorId = await testScope.CreateUser();
        var organization = await testScope.InitializeOrganization(
            userId,
            organization => organization
                .AddSpace(userId)
                .AddUser(participatorId, u => u
                    .SetSpaceIssuesAccessLevel(0, ChildrenAccessLevel.Create)));

        var statusWhereSpaceAccessMissing = organization.GetStatus(1, 0, 0);
        
        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => _issuesController
            .WithOrganizationAuthorization(organization.Id, participatorId)
            .Execute(x => x.Create(
                new CreateIssueRequest
                {
                    Content = "New Issue",
                    StatusId = statusWhereSpaceAccessMissing.Id
                })));
        
        var notFound = ex.HasInnerException<NotFoundException>();
        Assert.Equal($"Status: {statusWhereSpaceAccessMissing.Id} is not found, or Create permission is missing for Epic contains this status", notFound.Message);
    }
    
    [Fact]
    public async Task User_ShouldCreateIssue_WhenHasIssuesCreateChildrenAccessOnSpaceLevel()
    {
        using var testScope = host.CreateTestScope();
        var userId = await testScope.CreateUser();
        var participatorId = await testScope.CreateUser();
        var organization = await testScope.InitializeOrganization(
            userId,
            organization => organization
                .AddUser(participatorId, u => u
                    .SetSpaceIssuesAccessLevel(0, ChildrenAccessLevel.Create)));

        var status = organization.GetStatus(0, 0, 0);
        
        var issueId = await _issuesController
            .WithOrganizationAuthorization(organization.Id, participatorId)
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
    public async Task User_ShouldUpdateIssue_WhenIsOrganizationOwner()
    {
        using var testScope = host.CreateTestScope();
        var userId = await testScope.CreateUser();
        var organization = await testScope.InitializeOrganization(
            userId,
            o => o
                .AddIssueToDefaultStatus(userId, builder => builder.WithContent("Hi")));

        var issue = organization.GetIssue(0, 0, 0, 0);
        
        await _issuesController
            .WithOrganizationAuthorization(organization.Id, userId)
            .Execute(x => x.Update(
                issue.Id,
                new UpdateIssueRequest
                {
                    Content = "New",
                }));

        issue = await testScope.Database.Issues.FirstAsyncEF(e => e.Id == issue.Id);
        Assert.Equal("New", issue.Content);
    }
    
    [Fact]
    public async Task User_ShouldNotUpdateIssue_WhenHasNotAccess()
    {
        using var testScope = host.CreateTestScope();
        var userId = await testScope.CreateUser();
        var participatorId = await testScope.CreateUser();
        var organization = await testScope.InitializeOrganization(
            userId,
            o => o
                .AddUser(participatorId, u => u.SetIssuesAccessLevel(ChildrenAccessLevel.Create))
                .AddIssueToDefaultStatus(userId, builder => builder.WithContent("Hi")));

        var issue = organization.GetIssue(0, 0, 0, 0);
        
        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => _issuesController
            .WithOrganizationAuthorization(organization.Id, participatorId)
            .Execute(x => x.Update(
                issue.Id,
                new UpdateIssueRequest
                {
                    Content = "New",
                })));
        
        var notFound = ex.HasInnerException<NotFoundException>();
        Assert.Equal("Issue is not exists or epic children permission: Update is missing", notFound.Message);
    }
    
    [Fact]
    public async Task User_ShouldUpdateIssue_WhenHasGlobalAccessToUpdateIssues()
    {
        using var testScope = host.CreateTestScope();
        var userId = await testScope.CreateUser();
        var participatorId = await testScope.CreateUser();
        var organization = await testScope.InitializeOrganization(
            userId,
            o => o
                .AddUser(participatorId, u => u.SetIssuesAccessLevel(ChildrenAccessLevel.Update))
                .AddIssueToDefaultStatus(userId, builder => builder.WithContent("Hi")));

        var issue = organization.GetIssue(0, 0, 0, 0);
        
        await _issuesController
            .WithOrganizationAuthorization(organization.Id, participatorId)
            .Execute(x => x.Update(
                issue.Id,
                new UpdateIssueRequest
                {
                    Content = "New",
                }));

        issue = await testScope.Database.Issues.FirstAsyncEF(e => e.Id == issue.Id);
        Assert.Equal("New", issue.Content);
    }
    
    [Fact]
    public async Task User_ShouldUpdateIssue_WhenHasAccessToUpdateIssuesOnSpaceLevel()
    {
        using var testScope = host.CreateTestScope();
        var userId = await testScope.CreateUser();
        var participatorId = await testScope.CreateUser();
        var organization = await testScope.InitializeOrganization(
            userId,
            o => o
                .AddUser(participatorId, u => u.SetSpaceIssuesAccessLevel(0, ChildrenAccessLevel.Update))
                .AddIssueToDefaultStatus(userId, builder => builder.WithContent("Hi")));

        var issue = organization.GetIssue(0, 0, 0, 0);
        
        await _issuesController
            .WithOrganizationAuthorization(organization.Id, participatorId)
            .Execute(x => x.Update(
                issue.Id,
                new UpdateIssueRequest
                {
                    Content = "New",
                }));

        issue = await testScope.Database.Issues.FirstAsyncEF(e => e.Id == issue.Id);
        Assert.Equal("New", issue.Content);
    }
    
    [Fact]
    public async Task User_ShouldDeleteIssue_WhenIsOrganizationOwner()
    {
        using var testScope = host.CreateTestScope();
        var userId = await testScope.CreateUser();
        var organization = await testScope.InitializeOrganization(
            userId,
            o => o
                .AddIssueToDefaultStatus(userId));

        var issue = organization.GetIssue(0, 0, 0, 0);
        
        await _issuesController
            .WithOrganizationAuthorization(organization.Id, userId)
            .Execute(x => x.Delete(issue.Id));

        issue = await testScope.Database.Issues.FirstOrDefaultAsyncEF(e => e.Id == issue.Id);
        Assert.Null(issue);
    }
    
    [Fact]
    public async Task User_ShouldNotDeleteIssue_WhenHasNotAccess()
    {
        using var testScope = host.CreateTestScope();
        var userId = await testScope.CreateUser();
        var participatorId = await testScope.CreateUser();
        var organization = await testScope.InitializeOrganization(
            userId,
            o => o
                .AddUser(participatorId,  u => u.SetIssuesAccessLevel(ChildrenAccessLevel.Create))
                .AddIssueToDefaultStatus(userId, builder => builder.WithContent("Hi")));

        var issue = organization.GetIssue(0, 0, 0, 0);
        
        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => _issuesController
            .WithOrganizationAuthorization(organization.Id, participatorId)
            .Execute(x => x.Delete(issue.Id)));
        
        var notFound = ex.HasInnerException<NotFoundException>();
        Assert.Equal("Issue is not exists or epic children permission: Delete is missing", notFound.Message);
    }
    
    [Fact]
    public async Task User_ShouldDeleteIssue_WhenHasGlobalAccessToDeleteIssues()
    {
        using var testScope = host.CreateTestScope();
        var userId = await testScope.CreateUser();
        var participatorId = await testScope.CreateUser();
        var organization = await testScope.InitializeOrganization(
            userId,
            o => o
                .AddUser(participatorId,  u => u.SetIssuesAccessLevel(ChildrenAccessLevel.Delete))
                .AddIssueToDefaultStatus(userId));

        var issue = organization.GetIssue(0, 0, 0, 0);
        
        await _issuesController
            .WithOrganizationAuthorization(organization.Id, participatorId)
            .Execute(x => x.Delete(issue.Id));

        issue = await testScope.Database.Issues.FirstOrDefaultAsyncEF(e => e.Id == issue.Id);
        Assert.Null(issue);
    }

    [Fact]
    public async Task User_ShouldDeleteIssue_WhenHasAccessToDeleteIssuesOnSpaceLevel()
    {
        using var testScope = host.CreateTestScope();
        var userId = await testScope.CreateUser();
        var participatorId = await testScope.CreateUser();
        var organization = await testScope.InitializeOrganization(
            userId,
            o => o
                .AddUser(participatorId,  u => u.SetSpaceIssuesAccessLevel(0, ChildrenAccessLevel.Delete))
                .AddIssueToDefaultStatus(userId));

        var issue = organization.GetIssue(0, 0, 0, 0);
        
        await _issuesController
            .WithOrganizationAuthorization(organization.Id, participatorId)
            .Execute(x => x.Delete(issue.Id));

        issue = await testScope.Database.Issues.FirstOrDefaultAsyncEF(e => e.Id == issue.Id);
        Assert.Null(issue);
    }

    [Fact]
    public async Task User_ShouldMoveIssue_WhenIsOwner()
    {
        using var testScope = host.CreateTestScope();
        var userId = await testScope.CreateUser();
        var organization = await testScope.InitializeOrganization(
            userId,
            o => o
                .AddSpace(userId, s => s
                    .AddEpic(userId, e => e.AddStatus()))
                .AddIssueToDefaultStatus(userId));

        var issue = organization.GetIssue(0, 0, 0, 0);
        var newStatus = organization.GetStatus(1, 1, 1);
        
        await _issuesController
            .WithOrganizationAuthorization(organization.Id, userId)
            .Execute(x => x.Move(
                issue.Id,
                new MoveIssueRequest
                {
                    StatusId = newStatus.Id
                }));

        issue = await testScope.Database.Issues.FirstOrDefaultAsyncEF(e => e.Id == issue.Id);
        Assert.NotNull(issue);
        Assert.Equal(newStatus.Id, issue.StatusId);
    }

    [Fact]
    public async Task User_ShouldMoveIssue_WhenHasCreateIssuesAccessInEpicAndIssueUpdateAccess()
    {
        using var testScope = host.CreateTestScope();
        var userId = await testScope.CreateUser();
        var participatorId = await testScope.CreateUser();
        var organization = await testScope.InitializeOrganization(
            userId,
            o => o
                .AddUser(participatorId, u => u.SetIssuesAccessLevel(ChildrenAccessLevel.Create | ChildrenAccessLevel.Update))
                .AddSpace(userId, s => s
                    .AddEpic(userId, e => e.AddStatus()))
                .AddIssueToDefaultStatus(userId));

        var issue = organization.GetIssue(0, 0, 0, 0);
        var newStatus = organization.GetStatus(1, 1, 1);
        
        await _issuesController
            .WithOrganizationAuthorization(organization.Id, participatorId)
            .Execute(x => x.Move(
                issue.Id,
                new MoveIssueRequest
                {
                    StatusId = newStatus.Id
                }));

        issue = await testScope.Database.Issues.FirstOrDefaultAsyncEF(e => e.Id == issue.Id);
        Assert.NotNull(issue);
        Assert.Equal(newStatus.Id, issue.StatusId);
    }
}