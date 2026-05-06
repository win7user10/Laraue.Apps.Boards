using System.Net;
using Laraue.Apps.StructuredMessages.DataAccess.Enums;
using Laraue.Apps.StructuredMessages.IntegrationTests.Infrastructure;
using Laraue.Apps.StructuredMessages.WebApiHost.Controllers;
using Laraue.Apps.StructuredMessages.WebApiServices;
using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Laraue.Apps.StructuredMessages.IntegrationTests;

[Collection("IntegrationTest")]
public class SpacesControllerTests(WebApiTestHost host) : IClassFixture<WebApiTestHost>
{
    private readonly Proxy<SpacesController> _spacesController = host.Controller<SpacesController>();
    
    [Fact]
    public async Task User_ShouldCreateSpaceInOwnedOrganization_Always()
    {
        using var testScope = host.CreateTestScope();
        var userId = await testScope.CreateUser();
        var organization = await testScope.InitializeOrganization(userId);
        
        var spaceId = await _spacesController
            .WithOrganizationAuthorization(organization.Id, userId)
            .Execute(x => x.Create(
                new CreateSpaceRequest
                {
                    Name = "Space 1",
                    Color = "#ffffff"
                }));

        var spaces = await testScope.Database.Spaces.Include(x => x.Epics).ToListAsyncEF();
        
        var space = spaces.First(x => x.Id == spaceId);
        Assert.Equal("Space 1", space.Name);
        Assert.Equal("#ffffff", space.Color);
        Assert.Equal(userId, space.CreatorId);
        Assert.True(space.CreatedAt != default);
        Assert.True(space.UpdatedAt != default);
        Assert.False(space.IsDefault);
        
        var epic = Assert.Single(space.Epics!);
        Assert.True(epic.IsDefault);
    }
    
    [Fact]
    public async Task User_ShouldCreateSpaceInOrganization_WhenHasAccess()
    {
        using var testScope = host.CreateTestScope();
        var ownerId = await testScope.CreateUser();
        var participatorId = await testScope.CreateUser();
        var organization = await testScope.InitializeOrganization(ownerId, org => org
            .AddUser(participatorId, builder => builder
                .SetSpacesAccessLevel(ChildrenAccessLevel.Create)));
        
        var spaceId = await _spacesController
            .WithOrganizationAuthorization(organization.Id, participatorId)
            .Execute(x => x.Create(
                new CreateSpaceRequest
                {
                    Name = "Space 1",
                    Color = "#ffffff"
                }));

        var spaces = await testScope.Database.Spaces.Include(s => s.Users).ToListAsyncEF();
        var space = spaces.First(x => x.Id == spaceId);
        Assert.Equal(participatorId, space.CreatorId);
    }
    
    [Fact]
    public async Task User_ShouldNotCreateSpaceInOrganization_WhenHasNotAccess()
    {
        using var testScope = host.CreateTestScope();
        var ownerId = await testScope.CreateUser();
        var participatorId = await testScope.CreateUser();
        var organization = await testScope.InitializeOrganization(ownerId, org => org
            .AddUser(participatorId, builder => builder
                .SetSpacesAccessLevel(ChildrenAccessLevel.Read)));

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => _spacesController
            .WithOrganizationAuthorization(organization.Id, participatorId)
            .Execute(x => x.Create(
                new CreateSpaceRequest
                {
                    Name = "Space 1",
                    Color = "#ffffff"
                })));
        Assert.Equal(HttpStatusCode.NotFound, ex.StatusCode);
    }
    
    [Fact]
    public async Task User_ShouldViewSpacesInOwnedOrganization_Always()
    {
        using var testScope = host.CreateTestScope();
        var ownerId = await testScope.CreateUser();
        var participatorId = await testScope.CreateUser();
        var organization = await testScope.InitializeOrganization(ownerId, org => org
            .AddUser(participatorId)
            .AddSpace(participatorId, s => s
                .WithName("Space created by Participator")));

        var spaceId = organization.Spaces![1].Id;
        
        var spaces = await _spacesController
            .WithOrganizationAuthorization(organization.Id, ownerId)
            .Execute(x => x.GetAll());
        
        Assert.Equal(2, spaces!.Length);
        var space = spaces.First(x => x.Id == spaceId);
        Assert.Equal("Space created by Participator", space.Name);
        Assert.Equal(EntityAccessLevel.All, space.AccessLevel);
    }
    
    [Fact]
    public async Task User_ShouldViewSpacesInOrganization_WhenHasAccessOnOrganizationLevel()
    {
        using var testScope = host.CreateTestScope();
        var ownerId = await testScope.CreateUser();
        var participatorId = await testScope.CreateUser();
        var organization = await testScope.InitializeOrganization(ownerId, org => org
            .AddSpace(ownerId)
            .AddUser(participatorId, builder => builder
                .SetSpacesAccessLevel(ChildrenAccessLevel.Read)));
        
        var spaces = await _spacesController
            .WithOrganizationAuthorization(organization.Id, participatorId)
            .Execute(x => x.GetAll());
        
        Assert.Equal(2, spaces!.Length);
        Assert.All(spaces, s => Assert.Equal(EntityAccessLevel.Read, s.AccessLevel));
    }
    
    [Fact]
    public async Task User_ShouldViewSpacesInOrganization_WhenHasAccessOnSpacesLevel()
    {
        using var testScope = host.CreateTestScope();
        var ownerId = await testScope.CreateUser();
        var participatorId = await testScope.CreateUser();
        var organization = await testScope.InitializeOrganization(ownerId, org => org
            .AddSpace(ownerId)
            .AddUser(participatorId, builder => builder
                .SetSpacesAccessLevel(ChildrenAccessLevel.Read)));
        
        var spaces = await _spacesController
            .WithOrganizationAuthorization(organization.Id, participatorId)
            .Execute(x => x.GetAll());
        
        Assert.Equal(2, spaces!.Length);
        Assert.All(spaces, s => Assert.Equal(EntityAccessLevel.Read, s.AccessLevel));
    }
    
    [Fact]
    public async Task User_ShouldNotViewSpacesInOrganization_WhenHasNotAccess()
    {
        using var testScope = host.CreateTestScope();
        var ownerId = await testScope.CreateUser();
        var participatorId = await testScope.CreateUser();
        var organization = await testScope.InitializeOrganization(ownerId, org => org
            .AddSpace(ownerId)
            .AddUser(participatorId, builder => builder
                .SetSpacesAccessLevel(ChildrenAccessLevel.Read)));
        
        var spaces = await _spacesController
            .WithOrganizationAuthorization(organization.Id, participatorId)
            .Execute(x => x.GetAll());
        
        Assert.Equal(2, spaces!.Length);
        Assert.All(spaces, s => Assert.Equal(EntityAccessLevel.Read, s.AccessLevel));
    }
    
    [Fact]
    public async Task UserChildrenAccessLevel_ShouldBeMergedFromOrganizationAndSpaceLevel_Always()
    {
        using var testScope = host.CreateTestScope();
        var ownerId = await testScope.CreateUser();
        var participatorId = await testScope.CreateUser();
        var organization = await testScope.InitializeOrganization(ownerId, org => org
            .AddSpace(ownerId)
            .AddUser(participatorId, builder => builder
                .SetSpacesAccessLevel(ChildrenAccessLevel.Create)
                .SetSpaceAccessLevel(1, EntityAccessLevel.Update)));

        var spaceId = organization.Spaces![1].Id;
        
        var spaces = await _spacesController
            .WithOrganizationAuthorization(organization.Id, participatorId)
            .Execute(x => x.GetAll());
        
        var space = spaces!.First(x => x.Id == spaceId);
        Assert.Equal(EntityAccessLevel.Read | EntityAccessLevel.Update, space.AccessLevel);
    }
    
    [Fact]
    public async Task User_ShouldViewEpicsInOwnedOrganization_Always()
    {
        using var testScope = host.CreateTestScope();
        var ownerId = await testScope.CreateUser();
        var participatorId = await testScope.CreateUser();
        var organization = await testScope.InitializeOrganization(ownerId, org => org
            .AddUser(participatorId)
            .AddSpace(participatorId, s => s
                .WithName("Space created by Participator")));

        var spaceId = organization.Spaces![1].Id;
        
        var epics = await _spacesController
            .WithOrganizationAuthorization(organization.Id, ownerId)
            .Execute(x => x.GetSpaceEpics(spaceId));
        
        var epic = Assert.Single(epics!);
        Assert.Equal("Backlog", epic.Name);
        Assert.Equal(EntityAccessLevel.All, epic.EntityAccessLevel);
    }
    
    [Fact]
    public async Task User_ShouldViewEpics_WhenHasSpaceLevelPermission()
    {
        using var testScope = host.CreateTestScope();
        var ownerId = await testScope.CreateUser();
        var participatorId = await testScope.CreateUser();
        var organization = await testScope.InitializeOrganization(ownerId, org => org
            .AddUser(participatorId, b => b
                .SetSpaceAccessLevel(1, EntityAccessLevel.Read))
            .AddSpace(ownerId, s => s
                .AddEpic(ownerId)));

        var spaceId = organization.Spaces![1].Id;
        
        var epics = await _spacesController
            .WithOrganizationAuthorization(organization.Id, participatorId)
            .Execute(x => x.GetSpaceEpics(spaceId));
        
        Assert.Equal(2, epics!.Length);
    }
    
    [Fact]
    public async Task User_ShouldNotViewEpics_WhenHasAnotherSpaceLevelPermission()
    {
        using var testScope = host.CreateTestScope();
        var ownerId = await testScope.CreateUser();
        var participatorId = await testScope.CreateUser();
        var organization = await testScope.InitializeOrganization(ownerId, org => org
            .AddUser(participatorId, b => b
                .SetSpaceAccessLevel(0, EntityAccessLevel.Read))
            .AddSpace(ownerId, s => s
                .AddEpic(ownerId)));

        var spaceId = organization.Spaces![1].Id;
        
        var epics = await _spacesController
            .WithOrganizationAuthorization(organization.Id, participatorId)
            .Execute(x => x.GetSpaceEpics(spaceId));
        
        Assert.Empty(epics!);
    }
    
    [Fact]
    public async Task User_ShouldViewEpics_WhenHasEpicsLevelPermission()
    {
        using var testScope = host.CreateTestScope();
        var ownerId = await testScope.CreateUser();
        var participatorId = await testScope.CreateUser();
        var organization = await testScope.InitializeOrganization(ownerId, org => org
            .AddUser(participatorId, b => b
                .SetEpicsAccessLevel(ChildrenAccessLevel.Read))
            .AddSpace(ownerId, s => s
                .AddEpic(ownerId)));

        var spaceId = organization.Spaces![1].Id;
        
        var epics = await _spacesController
            .WithOrganizationAuthorization(organization.Id, participatorId)
            .Execute(x => x.GetSpaceEpics(spaceId));
        
        Assert.Equal(2, epics!.Length);
    }
    
    [Fact]
    public async Task User_ShouldViewEpics_WhenHasSpaceEpicsLevelPermission()
    {
        using var testScope = host.CreateTestScope();
        var ownerId = await testScope.CreateUser();
        var participatorId = await testScope.CreateUser();
        var organization = await testScope.InitializeOrganization(ownerId, org => org
            .AddUser(participatorId, b => b
                .SetSpaceEpicsAccessLevel(1, ChildrenAccessLevel.Read))
            .AddSpace(ownerId, s => s
                .AddEpic(ownerId)));

        var spaceId = organization.Spaces![1].Id;
        
        var epics = await _spacesController
            .WithOrganizationAuthorization(organization.Id, participatorId)
            .Execute(x => x.GetSpaceEpics(spaceId));
        
        Assert.Equal(2, epics!.Length);
    }
    
    [Fact]
    public async Task User_ShouldViewEpics_WhenHasEpicLevelWritePermission()
    {
        using var testScope = host.CreateTestScope();
        var ownerId = await testScope.CreateUser();
        var participatorId = await testScope.CreateUser();
        var organization = await testScope.InitializeOrganization(ownerId, org => org
            .AddUser(participatorId, b => b
                .SetEpicAccessLevel(1, 1, EntityAccessLevel.Delete))
            .AddSpace(ownerId, s => s
                .AddEpic(ownerId)));

        var spaceId = organization.Spaces![1].Id;
        
        var epics = await _spacesController
            .WithOrganizationAuthorization(organization.Id, participatorId)
            .Execute(x => x.GetSpaceEpics(spaceId));
        
        Assert.Equal(2, epics!.Length);
    }
}