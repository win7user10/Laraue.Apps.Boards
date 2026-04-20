using System.Net;
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
        Assert.Equal(8, organization.JoinCode.Length);
        Assert.True(organization.CreatedAt != default);
        Assert.True(organization.UpdatedAt != default);
    }
    
    [Fact]
    public async Task User_ShouldViewAvailableOrganizations_WhenHasAccess()
    {
        using var testScope = host.CreateTestScope();
        var userId = await testScope.CreateUser();

        testScope.Database.Organizations.Add(
            new Organization
            {
                Name = "Org 1",
                OwnerId = userId,
                Color = "#ffffff",
                Spaces = new List<Space>
                {
                    new()
                    {
                        Name = "Space 1",
                        CreatorId = userId,
                    }
                }
            });
        
        await testScope.Database.SaveChangesAsync();
        
        var organizationsResponse = await _organizationsController
            .WithAuthorization(userId)
            .Execute(x => x.GetOrganizations());
        
        var organization = Assert.Single(organizationsResponse!.Organizations);
        Assert.Equal("Org 1", organization.Name);
        Assert.Equal("#ffffff", organization.Color);
        Assert.Equal(1, organization.SpacesCount);
    }
    
    [Fact]
    public async Task User_ShouldNotViewOrganizations_WhenHasNotAccess()
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
                Spaces = new List<Space>
                {
                    new()
                    {
                        Name = "Space 1",
                        CreatorId = userId,
                    }
                }
            });
        
        await testScope.Database.SaveChangesAsync();
        
        var organizationsResponse = await _organizationsController
            .WithAuthorization(nonPermittedUserId)
            .Execute(x => x.GetOrganizations());
        
        Assert.Empty(organizationsResponse!.Organizations);
    }
    
    [Fact]
    public async Task User_ShouldUpdateOrganization_WhenHasAccess()
    {
        using var testScope = host.CreateTestScope();
        var userId = await testScope.CreateUser();

        var date1 = new DateTime(2020, 01, 01, 0, 0, 0, DateTimeKind.Utc);
        var entity = new Organization
        {
            Name = "Org 1",
            OwnerId = userId,
            Color = "#ffffff",
            CreatedAt = date1,
            UpdatedAt = date1,
        };
        
        testScope.Database.Organizations.Add(entity);
        await testScope.Database.SaveChangesAsync();
        
        await _organizationsController
            .WithAuthorization(userId)
            .Execute(x => x.Update(
                entity.Id,
                new EditOrganizationRequest
                {
                    Name = "Org 2",
                    Color = "#000000"
                }));

        var organizations = await testScope.Database.Organizations.ToListAsyncEF();
        
        var organization = Assert.Single(organizations);
        Assert.Equal("Org 2", organization.Name);
        Assert.Equal("#000000", organization.Color);
        Assert.Equal(date1, organization.CreatedAt);
        Assert.True(organization.UpdatedAt > date1);
    }
    
    [Fact]
    public async Task User_ShouldNotUpdateOrganization_WhenHasNoAccess()
    {
        using var testScope = host.CreateTestScope();
        var userId = await testScope.CreateUser();
        var nonPermittedUserId = await testScope.CreateUser();

        var entity = new Organization
        {
            Name = "Org 1",
            OwnerId = userId,
        };
        
        testScope.Database.Organizations.Add(entity);
        await testScope.Database.SaveChangesAsync();
        
        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => _organizationsController
            .WithAuthorization(nonPermittedUserId)
            .Execute(x => x.Update(
                entity.Id,
                new EditOrganizationRequest
                {
                    Name = "Org 2",
                    Color = "#000000"
                })));
        
        Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
    }
    
    [Fact]
    public async Task User_ShouldDeleteOrganization_WhenHasAccess()
    {
        using var testScope = host.CreateTestScope();
        var userId = await testScope.CreateUser();

        var entity = new Organization
        {
            Name = "Org 1",
            OwnerId = userId,
        };
        
        testScope.Database.Organizations.Add(entity);
        await testScope.Database.SaveChangesAsync();
        
        await _organizationsController
            .WithAuthorization(userId)
            .Execute(x => x.Delete(entity.Id));

        var organizations = await testScope.Database.Organizations.ToListAsyncEF();
        Assert.Empty(organizations);
    }
    
    [Fact]
    public async Task User_ShouldNotDeleteOrganization_WhenHasNoAccess()
    {
        using var testScope = host.CreateTestScope();
        var userId = await testScope.CreateUser();
        var nonPermittedUserId = await testScope.CreateUser();

        var entity = new Organization
        {
            Name = "Org 1",
            OwnerId = userId,
        };
        
        testScope.Database.Organizations.Add(entity);
        await testScope.Database.SaveChangesAsync();
        
        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => _organizationsController
            .WithAuthorization(nonPermittedUserId)
            .Execute(x => x.Delete(entity.Id)));
        
        Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
    }
    
    [Fact]
    public async Task User_ShouldJoinOrganization_WhenHasCode()
    {
        using var testScope = host.CreateTestScope();
        var ownerId = await testScope.CreateUser();
        var newUserId = await testScope.CreateUser();

        var organization = new Organization
        {
            Name = "Org 1",
            OwnerId = ownerId,
            JoinCode = "abc"
        };
        
        testScope.Database.Organizations.Add(organization);
        await testScope.Database.SaveChangesAsync();

        await _organizationsController
            .WithAuthorization(newUserId)
            .Execute(x => x.Join("abc"));
        
        var organizationUsers = await testScope.Database.OrganizationUsers.ToListAsyncEF();
        var organizationUser = Assert.Single(organizationUsers);
        
        Assert.Equal(organization.Id, organizationUser.OrganizationId);
        Assert.Equal(newUserId, organizationUser.UserId);
    }
}