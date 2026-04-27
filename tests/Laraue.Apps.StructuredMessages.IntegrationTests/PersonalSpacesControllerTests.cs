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
    
    [Fact]
    public async Task User_ShouldDeletePersonalSpace_Always()
    {
        using var testScope = host.CreateTestScope();
        var userId = await testScope.CreateUser();
        
        var organization = await new OrganizationInitializer(testScope.Database, userId)
            .WithType(OrganizationType.Personal)
            .AddSpace(userId)
            .Initialize();

        var spaceId = organization.Spaces![1].Id;
        
        await _epicsController
            .WithOrganizationAuthorization(organization.Id, userId)
            .Execute(x => x.Delete(spaceId));

        var spaces = await testScope.Database.Spaces.ToListAsyncEF();
        
        var space = spaces.FirstOrDefault(x => x.Id == spaceId);
        Assert.Null(space);
    }
    
    [Fact]
    public async Task User_ShouldViewPersonalOrganizationSpaces_Always()
    {
        using var testScope = host.CreateTestScope();
        
        // User 1 has organization with default space + additional space
        var user1Id = await testScope.CreateUser();
        var organization1 = await new OrganizationInitializer(testScope.Database, user1Id)
            .WithType(OrganizationType.Personal)
            .AddSpace(user1Id)
            .Initialize();
        var organization1SpaceIds = organization1.Spaces!.Select(x => x.Id);
        
        // User 2 has organization with default space
        var user2Id = await testScope.CreateUser();
        var organization2 = await new OrganizationInitializer(testScope.Database, user2Id)
            .WithType(OrganizationType.Personal)
            .Initialize();
        var organization2SpaceIds = organization2.Spaces!.Select(x => x.Id);
        
        // User 1 see two spaces
        var spaces = await _epicsController
            .WithOrganizationAuthorization(organization1.Id, user1Id)
            .Execute(x => x.GetAll());
        Assert.Equal(2, spaces!.Length);
        Assert.Equivalent(organization1SpaceIds, spaces.Select(x => x.Id));
        
        // User 2 see one space
        spaces = await _epicsController
            .WithOrganizationAuthorization(organization2.Id, user2Id)
            .Execute(x => x.GetAll());
        Assert.Single(spaces!);
        Assert.Equivalent(organization2SpaceIds, spaces!.Select(x => x.Id));
    }
    
    [Fact]
    public async Task User_ShouldViewAllFieldsOrPersonalSpace_Always()
    {
        using var testScope = host.CreateTestScope();
        var userId = await testScope.CreateUser();
        var organization = await new OrganizationInitializer(testScope.Database, userId)
            .WithType(OrganizationType.Personal)
            .AddSpace(userId,  space => space
                .WithColor("#ff11ff")
                .WithName("My Space"))
            .Initialize();
        var spaceId = organization.Spaces![1].Id;
        
        var spaces = await _epicsController
            .WithOrganizationAuthorization(organization.Id, userId)
            .Execute(x => x.GetAll());
        
        var space = spaces!.First(x => x.Id == spaceId);
        Assert.Equal("#ff11ff", space.Color);
        Assert.Equal("My Space", space.Name);
    }
}