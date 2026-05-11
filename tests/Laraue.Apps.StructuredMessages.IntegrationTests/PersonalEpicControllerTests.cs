using Laraue.Apps.StructuredMessages.IntegrationTests.Infrastructure;
using Laraue.Apps.StructuredMessages.WebApiHost.Controllers;
using Laraue.Apps.StructuredMessages.WebApiServices;
using Laraue.Core.Exceptions.Web;
using LinqToDB.EntityFrameworkCore;

namespace Laraue.Apps.StructuredMessages.IntegrationTests;

[Collection("IntegrationTest")]
public class PersonalEpicControllerTests(WebApiTestHost host) : IClassFixture<WebApiTestHost>
{
    private readonly Proxy<EpicsController> _epicsController = host.Controller<EpicsController>();
    
    [Fact]
    public async Task User_ShouldCreatePersonalEpic_Always()
    {
        using var testScope = host.CreateTestScope();
        var userId = await testScope.CreateUser();
        var organization = await testScope.InitializePersonalOrganization(
            userId,
            setup => setup.AddSpace(userId));

        var spaceId = organization.Spaces![1].Id;
        
        var epicId = await _epicsController
            .WithOrganizationAuthorization(organization.Id, userId)
            .Execute(x => x.Create(
                new CreateEpicRequest
                {
                    Name = "Epic 1",
                    Color = "#ffffff",
                    SpaceId = spaceId,
                }));

        var epic = await testScope.Database.Epics.FirstAsyncEF(e => e.Id == epicId);
        
        Assert.Equal("Epic 1", epic.Name);
        Assert.Equal("#ffffff", epic.Color);
        Assert.Equal(userId, epic.UserId);
        Assert.True(epic.CreatedAt != default);
        Assert.True(epic.UpdatedAt != default);
        Assert.True(epic.TouchedAt != default);
        Assert.False(epic.IsDefault);
    }
    
    [Fact]
    public async Task User_ShouldUpdatePersonalAdditionalEpic_Always()
    {
        using var testScope = host.CreateTestScope();
        var userId = await testScope.CreateUser();
        var organization = await testScope.InitializePersonalOrganization(
            userId,
            setup => setup
                .AddSpace(userId, spaceBuilder =>
                    spaceBuilder
                        .AddEpic(userId, epicBuilder => epicBuilder.WithName("My Epic").WithColor("#111111"))));

        var epicId = organization.Spaces![1].Epics![1].Id;
        
        await _epicsController
            .WithOrganizationAuthorization(organization.Id, userId)
            .Execute(x => x.Update(
                epicId,
                new ()
                {
                    Name = "Epic 1",
                    Color = "#ffffff",
                }));

        var epic = await testScope.Database.Epics.FirstAsyncEF(e => e.Id == epicId);
        
        Assert.Equal("Epic 1", epic.Name);
        Assert.Equal("#ffffff", epic.Color);
    }
    
    [Fact]
    public async Task User_ShouldUpdatePersonalDefaultEpic_Always()
    {
        using var testScope = host.CreateTestScope();
        var userId = await testScope.CreateUser();
        var organization = await testScope.InitializePersonalOrganization(userId);
        
        var epicId = organization.GetEpic(0, 0).Id;
        
        await _epicsController
            .WithOrganizationAuthorization(organization.Id, userId)
            .Execute(x => x.Update(
                epicId,
                new ()
                {
                    Name = "Epic 1",
                    Color = "#ffffff",
                }));

        var epic = await testScope.Database.Epics.FirstAsyncEF(e => e.Id == epicId);
        Assert.Equal("Epic 1", epic.Name);
    }
    
    [Fact]
    public async Task User_ShouldDeletePersonalAdditionalEpic_Always()
    {
        using var testScope = host.CreateTestScope();
        var userId = await testScope.CreateUser();
        var organization = await testScope.InitializePersonalOrganization(
            userId,
            setup => setup
                .AddSpace(userId, spaceBuilder =>
                    spaceBuilder
                        .AddEpic(userId, epicBuilder => epicBuilder.WithName("My Epic").WithColor("#111111")
                            .AddStatus(s => s.WithName("In Progress"))
                            .AddIssue(userId, 1, issue => issue.WithContent("Issue 1")))));

        var exceptedNewStatusId = organization.Spaces![1].Epics![0].Statuses![0].Id;
        var epicId = organization.Spaces![1].Epics![1].Id;
        
        await _epicsController
            .WithOrganizationAuthorization(organization.Id, userId)
            .Execute(x => x.Delete(epicId));

        var epic = await testScope.Database.Epics.FirstOrDefaultAsyncEF(e => e.Id == epicId);
        Assert.Null(epic);
     
        var issue = await testScope.Database.Issues.SingleAsyncEF();
        Assert.Equal(exceptedNewStatusId, issue.StatusId);
    }
    
    [Fact]
    public async Task User_ShouldNotDeletePersonalDefaultEpic_Always()
    {
        using var testScope = host.CreateTestScope();
        var userId = await testScope.CreateUser();
        var organization = await testScope.InitializePersonalOrganization(userId);

        var epicId = organization.GetEpic(0, 0).Id;

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => _epicsController
            .WithOrganizationAuthorization(organization.Id, userId)
            .Execute(x => x.Delete(epicId)));

        var badRequest = ex.HasInnerException<ForbiddenException>();
        Assert.Equal("Default Epic can not be deleted", badRequest.Message);
    }
    
    [Fact]
    public async Task User_ShouldGetPersonalDefaultEpic_Always()
    {
        using var testScope = host.CreateTestScope();
        var userId = await testScope.CreateUser();
        var organization = await testScope.InitializePersonalOrganization(userId);

        var epicId = organization.GetEpic(0, 0).Id;
        
        var epic = await _epicsController
            .WithOrganizationAuthorization(organization.Id, userId)
            .Execute(x => x.Get(epicId));
        
        Assert.NotNull(epic);
        Assert.False(epic.CanDelete);
        Assert.True(epic.CanUpdate);
    }
    
    [Fact]
    public async Task User_ShouldGetPersonalAdditionalEpic_Always()
    {
        using var testScope = host.CreateTestScope();
        var userId = await testScope.CreateUser();
        var organization = await testScope.InitializePersonalOrganization(userId,
            setup => setup
                .AddSpace(userId, spaceBuilder =>
                    spaceBuilder
                        .AddEpic(userId, epicBuilder => epicBuilder.WithName("My Epic").WithColor("#111111"))));

        var epicId = organization.GetEpic(1, 1).Id;
        
        var epic = await _epicsController
            .WithOrganizationAuthorization(organization.Id, userId)
            .Execute(x => x.Get(epicId));
        
        Assert.NotNull(epic);
        Assert.True(epic.CanDelete);
        Assert.True(epic.CanUpdate);
        Assert.True(epic.CanCreateIssues);
        Assert.True(epic.CanViewIssues);
        Assert.True(epic.CanDeleteIssues);
        Assert.True(epic.CanUpdateIssues);
    }
}