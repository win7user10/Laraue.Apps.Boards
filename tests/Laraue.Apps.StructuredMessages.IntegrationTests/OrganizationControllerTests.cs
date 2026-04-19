using Laraue.Apps.StructuredMessages.DataAccess.Models;
using Laraue.Apps.StructuredMessages.IntegrationTests.Infrastructure;
using Laraue.Apps.StructuredMessages.WebApiHost.Controllers;
using Laraue.Apps.StructuredMessages.WebApiServices;
using LinqToDB.EntityFrameworkCore;

namespace Laraue.Apps.StructuredMessages.IntegrationTests;

[Collection("IntegrationTest")]
public class OrganizationControllerTests(WebApiTestHost host) : IClassFixture<WebApiTestHost>
{
    private readonly Proxy<OrganizationsController> _organizationsController = host.Controller<OrganizationsController>();

    [Fact]
    public async Task CreateOrganization_ShouldCreateNewOrganization_Always()
    {
        using var testScope = host.CreateTestScope();
        var userId = await testScope.CreateUser();
        
        var newId = await _organizationsController
            .WithAuthorization(userId)
            .Execute(x => x.Create(
                new CreateOrganizationRequest
                {
                    Name = "Org 1",
                    Color = "#ffffff"
                }));

        var organizations = await testScope.Database.Organizations.ToListAsyncEF();
        
        var organization = Assert.Single(organizations);
        Assert.Equal("Org 1", organization.Name);
        Assert.Equal("#ffffff", organization.Color);
        Assert.Equal(userId, organization.OwnerId);
        Assert.Equal(newId, organization.Id);
        Assert.True(organization.CreatedAt != default);
        Assert.True(organization.UpdatedAt != default);
    }
    
    [Fact]
    public async Task User_ShouldViewAvailableOrganizations_WhenHasPermissions()
    {
        using var testScope = host.CreateTestScope();
        var userId = await testScope.CreateUser();

        testScope.Database.Organizations.Add(
            new Organization
            {
                Name = "Org 1",
                OwnerId = userId,
                Color = "#ffffff",
            });
        
        await testScope.Database.SaveChangesAsync();
        
        var organizationsResponse = await _organizationsController
            .WithAuthorization(userId)
            .Execute(x => x.GetOrganizations());
        
        var organization = Assert.Single(organizationsResponse!.Organizations);
        Assert.Equal("Org 1", organization.Name);
        Assert.Equal("#ffffff", organization.Color);
        Assert.Equal(0, organization.SpacesCount);
    }
    
    [Fact]
    public async Task User_ShouldNotViewOrganizations_WhenHasNotPermissions()
    {
        using var testScope = host.CreateTestScope();
        var userId = await testScope.CreateUser();
        var nonPermittedUserId = await testScope.CreateUser();

        testScope.Database.Organizations.Add(
            new Organization
            {
                Name = "Org 1",
                OwnerId = userId,
                Color = "#ffffff",
            });
        
        await testScope.Database.SaveChangesAsync();
        
        var organizationsResponse = await _organizationsController
            .WithAuthorization(nonPermittedUserId)
            .Execute(x => x.GetOrganizations());
        
        Assert.Empty(organizationsResponse!.Organizations);
    }
}