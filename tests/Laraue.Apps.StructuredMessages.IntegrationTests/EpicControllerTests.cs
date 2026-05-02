using Laraue.Apps.StructuredMessages.DataAccess.Enums;
using Laraue.Apps.StructuredMessages.IntegrationTests.Infrastructure;
using Laraue.Apps.StructuredMessages.WebApiHost.Controllers;
using Laraue.Apps.StructuredMessages.WebApiServices;
using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Laraue.Apps.StructuredMessages.IntegrationTests;

[Collection("IntegrationTest")]
public class EpicControllerTests(WebApiTestHost host) : IClassFixture<WebApiTestHost>
{
    private readonly Proxy<EpicsController> _epicsController = host.Controller<EpicsController>();
    
    [Fact]
    public async Task User_ShouldCreateEpicInOwnedOrganization_Always()
    {
        using var testScope = host.CreateTestScope();
        var userId = await testScope.CreateUser();
        var organization = await testScope.InitializeOrganization(userId, org => org
            .AddSpace(userId));
        
        var spaceId = organization.Spaces![1].Id;
        var epicId = await _epicsController
            .WithOrganizationAuthorization(organization.Id, userId)
            .Execute(x => x.Create(
                new CreateEpicRequest
                {
                    Name = "Epic 1",
                    Color = "#fffff1",
                    SpaceId = spaceId,
                }));

        var epics = await testScope.Database.Epics
            .Include(e => e.Statuses)
            .ToListAsyncEF();
        
        var epic = epics.First(x => x.Id == epicId);
        Assert.Equal("Epic 1", epic.Name);
        Assert.Equal("#fffff1", epic.Color);
        Assert.Equal(userId, epic.UserId);
        Assert.True(epic.CreatedAt != default);
        Assert.True(epic.UpdatedAt != default);
        Assert.True(epic.TouchedAt != default);
        Assert.False(epic.IsDefault);
        
        var status = Assert.Single(epic.Statuses!);
        Assert.Equal("New", status.Name);
    }
    
    [Fact]
    public async Task User_ShouldCreateEpicInOrganization_WhenHasAccessOnOrganizationLevel()
    {
        using var testScope = host.CreateTestScope();
        var ownerId = await testScope.CreateUser();
        var participatorId = await testScope.CreateUser();
        var organization = await testScope.InitializeOrganization(ownerId, org => org
            .AddUser(participatorId, builder => builder
                .SetEpicsAccessLevel(ChildrenAccessLevel.Create)));
        
        var spaceId = organization.Spaces![0].Id;
        
        var epicId = await _epicsController
            .WithOrganizationAuthorization(organization.Id, participatorId)
            .Execute(x => x.Create(
                new CreateEpicRequest
                {
                    Name = "Epic 1",
                    Color = "#fffff1",
                    SpaceId = spaceId,
                }));
        
        Assert.NotEqual(0, epicId);
    }
    
    [Fact]
    public async Task User_ShouldCreateEpicInOrganization_WhenHasAccessOnSpacesLevel()
    {
        using var testScope = host.CreateTestScope();
        var ownerId = await testScope.CreateUser();
        var participatorId = await testScope.CreateUser();
        var organization = await testScope.InitializeOrganization(ownerId, org => org
            .AddUser(participatorId, builder => builder
                .SetSpacesAccessLevel(ChildrenAccessLevel.Create)));
        
        var spaceId = organization.Spaces![0].Id;
        
        var epicId = await _epicsController
            .WithOrganizationAuthorization(organization.Id, participatorId)
            .Execute(x => x.Create(
                new CreateEpicRequest
                {
                    Name = "Epic 1",
                    Color = "#fffff1",
                    SpaceId = spaceId,
                }));
        
        Assert.NotEqual(0, epicId);
    }
    
    [Fact]
    public async Task User_ShouldCreateEpicInOrganization_WhenHasAccessOnSpaceLevel()
    {
        using var testScope = host.CreateTestScope();
        var ownerId = await testScope.CreateUser();
        var participatorId = await testScope.CreateUser();
        var organization = await testScope.InitializeOrganization(ownerId, org => org
            .AddUser(participatorId, builder => builder
                .SetSpaceEpicsAccessLevel(0, ChildrenAccessLevel.Create)));
        
        var spaceId = organization.Spaces![0].Id;
        
        var epicId = await _epicsController
            .WithOrganizationAuthorization(organization.Id, participatorId)
            .Execute(x => x.Create(
                new CreateEpicRequest
                {
                    Name = "Epic 1",
                    Color = "#fffff1",
                    SpaceId = spaceId,
                }));
        
        Assert.NotEqual(0, epicId);
    }
}