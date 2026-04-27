using System.Net;
using Laraue.Apps.StructuredMessages.DataAccess.Enums;
using Laraue.Apps.StructuredMessages.IntegrationTests.Infrastructure;
using Laraue.Apps.StructuredMessages.WebApiHost.Controllers;
using Laraue.Apps.StructuredMessages.WebApiServices;
using LinqToDB.EntityFrameworkCore;

namespace Laraue.Apps.StructuredMessages.IntegrationTests;

[Collection("IntegrationTest")]
public class SpacesControllerTests(WebApiTestHost host) : IClassFixture<WebApiTestHost>
{
    private readonly Proxy<PersonalSpacesController> _spacesController = host.Controller<PersonalSpacesController>();
    
    [Fact]
    public async Task User_ShouldCreateSpaceInOwnedOrganization_Always()
    {
        using var testScope = host.CreateTestScope();
        var userId = await testScope.CreateUser();
        var organization = await new OrganizationInitializer(testScope.Database, userId).Initialize();
        
        var spaceId = await _spacesController
            .WithOrganizationAuthorization(organization.Id, userId)
            .Execute(x => x.Create(
                new CreateSpaceRequest
                {
                    Name = "Space 1",
                    Color = "#ffffff"
                }));

        var spaces = await testScope.Database.Spaces.ToListAsyncEF();
        
        var space = spaces.First(x => x.Id == spaceId);
        Assert.Equal("Space 1", space.Name);
        Assert.Equal("#ffffff", space.Color);
        Assert.Equal(userId, space.CreatorId);
        Assert.True(space.CreatedAt != default);
        Assert.True(space.UpdatedAt != default);
    }
    
    [Fact]
    public async Task User_ShouldCreateSpaceInOrganization_WhenHasAccess()
    {
        using var testScope = host.CreateTestScope();
        var ownerId = await testScope.CreateUser();
        var participatorId = await testScope.CreateUser();
        var organization = await new OrganizationInitializer(testScope.Database, ownerId)
            .AddUser(participatorId, builder => builder.SetOrganizationAccessLevel(ItemsAccessLevel.Create))
            .Initialize();
        
        var spaceId = await _spacesController
            .WithOrganizationAuthorization(organization.Id, participatorId)
            .Execute(x => x.Create(
                new CreateSpaceRequest
                {
                    Name = "Space 1",
                    Color = "#ffffff"
                }));

        var spaces = await testScope.Database.Spaces.ToListAsyncEF();
        
        var space = spaces.First(x => x.Id == spaceId);
        Assert.Equal(participatorId, space.CreatorId);
    }
    
    [Fact]
    public async Task User_ShouldNotCreateSpaceInOrganization_WhenHasNotAccess()
    {
        using var testScope = host.CreateTestScope();
        var ownerId = await testScope.CreateUser();
        var participatorId = await testScope.CreateUser();
        var organization = await new OrganizationInitializer(testScope.Database, ownerId)
            .AddUser(participatorId, builder => builder.SetOrganizationAccessLevel(ItemsAccessLevel.Read))
            .Initialize();

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
}