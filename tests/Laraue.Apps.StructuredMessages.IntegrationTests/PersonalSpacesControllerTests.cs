using Laraue.Apps.StructuredMessages.DataAccess.Models;
using Laraue.Apps.StructuredMessages.IntegrationTests.Infrastructure;
using Laraue.Apps.StructuredMessages.WebApiHost.Controllers;
using Laraue.Apps.StructuredMessages.WebApiServices;
using LinqToDB.EntityFrameworkCore;

namespace Laraue.Apps.StructuredMessages.IntegrationTests;

[Collection("IntegrationTest")]
public class PersonalSpacesControllerTests(WebApiTestHost host) : IClassFixture<WebApiTestHost>
{
    private readonly Proxy<PersonalSpacesController> _epicsController = host.Controller<PersonalSpacesController>();

    [Fact]
    public async Task User_ShouldCreatePersonalSpace_Always()
    {
        using var testScope = host.CreateTestScope();
        var userId = await testScope.CreateUser();
        var organization = await testScope.InitializePersonalOrganization(userId);
        
        var spaceId = await _epicsController
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
        Assert.Equal(spaceId, space.Id);
        Assert.True(space.CreatedAt != default);
        Assert.True(space.UpdatedAt != default);
    }
    
    [Fact]
    public async Task User_ShouldUpdatePersonalSpace_Always()
    {
        using var testScope = host.CreateTestScope();
        var userId = await testScope.CreateUser();
        var timestamp = DateTime.UtcNow;
        
        var organization = await new OrganizationInitializer(testScope.Database, userId)
            .WithType(OrganizationType.Personal)
            .AddSpace(userId, space => space.WithTimestamp(timestamp))
            .Initialize();

        var spaceId = organization.Spaces![1].Id;
        
        await _epicsController
            .WithOrganizationAuthorization(organization.Id, userId)
            .Execute(x => x.Update(
                spaceId,
                new UpdateSpaceRequest
                {
                    Name = "Space 1",
                    Color = "#ffffff"
                }));

        var spaces = await testScope.Database.Spaces.ToListAsyncEF();
        
        var space = spaces.First(x => x.Id == spaceId);
        Assert.Equal("Space 1", space.Name);
        Assert.Equal("#ffffff", space.Color);
        Assert.Equal(userId, space.CreatorId);
        Assert.Equal(spaceId, space.Id);
        Assert.True(space.CreatedAt != default);
        Assert.True(space.UpdatedAt != default);
    }
}