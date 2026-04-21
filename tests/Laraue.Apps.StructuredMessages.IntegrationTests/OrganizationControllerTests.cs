using System.Net;
using Laraue.Apps.StructuredMessages.DataAccess.Enums;
using Laraue.Apps.StructuredMessages.DataAccess.Models;
using Laraue.Apps.StructuredMessages.IntegrationTests.Infrastructure;
using Laraue.Apps.StructuredMessages.Services;
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
    public async Task User_ShouldViewOwnedAndParticipatingOrganizations_Always()
    {
        using var testScope = host.CreateTestScope();
        var userId = await testScope.CreateUser();
        var user2Id = await testScope.CreateUser();

        testScope.Database.Organizations.AddRange(
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
                },
                Users = new List<OrganizationUser>
                {
                    new ()
                    {
                        UserId = user2Id,
                        AccessLevel = AccessLevel.Update,
                    }
                }
            },
            new Organization
            {
                Name = "Org 2",
                OwnerId = user2Id,
                Color = "#000000",
                Spaces = new List<Space>
                {
                    new()
                    {
                        Name = "Space 2",
                        CreatorId = userId,
                    }
                }
            });
        
        await testScope.Database.SaveChangesAsync();
        
        // First user see only owned organization
        var userOrganizationsResponse = await _organizationsController
            .WithAuthorization(userId)
            .Execute(x => x.GetOrganizations());
        var organizations = userOrganizationsResponse!.Organizations;
        var organization = Assert.Single(organizations);
        
        Assert.Equal(AccessLevel.Manage, organization.AccessLevel);
        Assert.Equal("Org 1", organization.Name);
        Assert.Equal("#ffffff", organization.Color);
        Assert.Equal(1, organization.SpacesCount);
        
        // Second user see owned organization and organization where he was added
        var user2OrganizationsResponse = await _organizationsController
            .WithAuthorization(user2Id)
            .Execute(x => x.GetOrganizations());
        
        organizations = user2OrganizationsResponse!.Organizations;
        Assert.Equal(2, organizations.Length);
        var (organization1, organization2) = (organizations[0], organizations[1]);
        
        Assert.Equal(AccessLevel.Update, organization1.AccessLevel);
        Assert.Equal("Org 1", organization1.Name);
        Assert.Equal("#ffffff", organization1.Color);
        Assert.Equal(1, organization1.SpacesCount);
        
        Assert.Equal(AccessLevel.Manage, organization2.AccessLevel);
        Assert.Equal("Org 2", organization2.Name);
        Assert.Equal("#000000", organization2.Color);
        Assert.Equal(1, organization2.SpacesCount);
    }
    
    [Fact]
    public async Task User_ShouldNotViewUnavailableOrganizations_Always()
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
    public async Task User_ShouldJoinOrganization_WhenHasCorrectCode()
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
    
    [Fact]
    public async Task User_ShouldNotJoinOrganization_WhenCodeIsWrong()
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

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => _organizationsController
            .WithAuthorization(newUserId)
            .Execute(x => x.Join("def")));
        
        Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
    }

    [Fact]
    public async Task User_ShouldSetEmployeeDirectAccess_WhenHeIsOwner()
    {
        using var testScope = host.CreateTestScope();
        var ownerId = await testScope.CreateUser();
        var userIdToReceivePermissions = await testScope.CreateUser();

        var organizationUser = new OrganizationUser { UserId = userIdToReceivePermissions };
        var epic = new Epic { Name = "Epic 1", UserId = ownerId };
        var space = new Space { Name = "Space 1", CreatorId = ownerId, Epics = new List<Epic> { epic } };
        
        var organization = new Organization
        {
            OwnerId = ownerId,
            Users = new List<OrganizationUser> { organizationUser },
            Spaces = new List<Space> { space }
        };
        
        testScope.Database.Add(organization);
        await testScope.Database.SaveChangesAsync();

        var request = new SetPermissionsRequest
        {
            OrganizationUserId = organizationUser.Id,
            Permissions = new Permissions
            {
                OrganizationAccessLevel = AccessLevel.None,
                EpicsAccessLevels = new AccessLevels
                {
                    DirectAccess = new Dictionary<long, AccessLevel>
                    {
                        [epic.Id] = AccessLevel.Create,
                    }
                },
                SpacesAccessLevels = new AccessLevels
                {
                    DirectAccess = new Dictionary<long, AccessLevel>
                    {
                        [space.Id] = AccessLevel.Update,
                    }
                }
            }
        };
        
        await _organizationsController
            .WithAuthorization(ownerId)
            .Execute(x => x.SetPermissions(request));
        
        var organizationUsers = await testScope.Database.OrganizationUsers.ToListAsyncEF();
        organizationUser = Assert.Single(organizationUsers);
        Assert.Equal(AccessLevel.None, organizationUser.AccessLevel);
        
        var spaceOrganizationUsers = await testScope.Database.SpaceOrganizationUsers.ToListAsyncEF();
        var spaceOrganizationUser = Assert.Single(spaceOrganizationUsers);
        Assert.Equal(AccessLevel.Update, spaceOrganizationUser.AccessLevel);
        Assert.Equal(organizationUser.Id, spaceOrganizationUser.OrganizationUserId);
        Assert.Equal(space.Id, spaceOrganizationUser.SpaceId);
        
        var epicOrganizationUsers = await testScope.Database.EpicOrganizationUsers.ToListAsyncEF();
        var epicOrganizationUser = Assert.Single(epicOrganizationUsers);
        Assert.Equal(AccessLevel.Create, epicOrganizationUser.AccessLevel);
        Assert.Equal(organizationUser.Id, epicOrganizationUser.OrganizationUserId);
        Assert.Equal(epic.Id, epicOrganizationUser.EpicId);
    }
    
    [Fact]
    public async Task User_ShouldSetEmployeeSectionAccess_WhenHeIsOwner()
    {
        using var testScope = host.CreateTestScope();
        var ownerId = await testScope.CreateUser();
        var userIdToReceivePermissions = await testScope.CreateUser();

        var organizationUser = new OrganizationUser { UserId = userIdToReceivePermissions };
        
        var organization = new Organization
        {
            OwnerId = ownerId,
            Users = new List<OrganizationUser> { organizationUser },
        };
        
        testScope.Database.Add(organization);
        await testScope.Database.SaveChangesAsync();

        var request = new SetPermissionsRequest
        {
            OrganizationUserId = organizationUser.Id,
            Permissions = new Permissions
            {
                OrganizationAccessLevel = AccessLevel.None,
                EpicsAccessLevels = new AccessLevels
                {
                    AccessLevel = AccessLevel.Delete,
                },
                SpacesAccessLevels = new AccessLevels
                {
                    AccessLevel = AccessLevel.Read,
                }
            }
        };
        
        await _organizationsController
            .WithAuthorization(ownerId)
            .Execute(x => x.SetPermissions(request));
        
        var organizationUsers = await testScope.Database.OrganizationUsers.ToListAsyncEF();
        organizationUser = Assert.Single(organizationUsers);
        Assert.Equal(AccessLevel.None, organizationUser.AccessLevel);
        
        var spaceOrganizationUsers = await testScope.Database.SpaceOrganizationUsers.ToListAsyncEF();
        var spaceOrganizationUser = Assert.Single(spaceOrganizationUsers);
        Assert.Equal(AccessLevel.Read, spaceOrganizationUser.AccessLevel);
        Assert.Equal(organizationUser.Id, spaceOrganizationUser.OrganizationUserId);
        Assert.Null(spaceOrganizationUser.SpaceId);
        
        var epicOrganizationUsers = await testScope.Database.EpicOrganizationUsers.ToListAsyncEF();
        var epicOrganizationUser = Assert.Single(epicOrganizationUsers);
        Assert.Equal(AccessLevel.Delete, epicOrganizationUser.AccessLevel);
        Assert.Equal(organizationUser.Id, epicOrganizationUser.OrganizationUserId);
        Assert.Null(epicOrganizationUser.EpicId);
    }
    
    [Fact]
    public async Task User_ShouldNotSetEmployeeAccess_WhenHeIsNotOwner()
    {
        using var testScope = host.CreateTestScope();
        var ownerId = await testScope.CreateUser();
        var userIdToReceivePermissions = await testScope.CreateUser();

        var organizationUser = new OrganizationUser { UserId = userIdToReceivePermissions };
        
        var organization = new Organization
        {
            OwnerId = ownerId,
            Users = new List<OrganizationUser> { organizationUser },
        };
        
        testScope.Database.Add(organization);
        await testScope.Database.SaveChangesAsync();

        var request = new SetPermissionsRequest
        {
            OrganizationUserId = organizationUser.Id,
            Permissions = new Permissions
            {
                OrganizationAccessLevel = AccessLevel.None,
                EpicsAccessLevels = new AccessLevels
                {
                    AccessLevel = AccessLevel.Delete,
                },
                SpacesAccessLevels = new AccessLevels
                {
                    AccessLevel = AccessLevel.Read,
                }
            }
        };
        
        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => _organizationsController
            .WithAuthorization(userIdToReceivePermissions)
            .Execute(x => x.SetPermissions(request)));
        Assert.Equal(HttpStatusCode.NotFound, ex.StatusCode);
    }
    
    [Fact]
    public async Task User_ShouldViewPermissionsOfNewEmployee_WhenHeIsOwner()
    {
        using var testScope = host.CreateTestScope();
        var ownerId = await testScope.CreateUser();
        var organizationUserId = await testScope.CreateUser();

        var organizationUser = new OrganizationUser { UserId = organizationUserId };
        var organization = new Organization
        {
            OwnerId = ownerId,
            Users = new List<OrganizationUser> { organizationUser },
        };
        
        testScope.Database.Add(organization);
        await testScope.Database.SaveChangesAsync();
        
        var permissions = await _organizationsController
            .WithAuthorization(ownerId)
            .Execute(x => x.GetPermissions(
                new GetPermissionsRequest
                {
                    OrganizationUserId = organizationUser.Id
                }));
        
        Assert.NotNull(permissions);
        Assert.Equal(AccessLevel.None, permissions.OrganizationAccessLevel);
        
        Assert.NotNull(permissions.SpacesAccessLevels);
        Assert.Null(permissions.SpacesAccessLevels.DirectAccess);
        Assert.Equal(AccessLevel.None, permissions.SpacesAccessLevels.AccessLevel);
        
        Assert.NotNull(permissions.EpicsAccessLevels);
        Assert.Null(permissions.EpicsAccessLevels.DirectAccess);
        Assert.Equal(AccessLevel.None, permissions.EpicsAccessLevels.AccessLevel);
    }
    
    [Fact]
    public async Task User_ShouldViewEmployeePermissions_WhenHeIsOwner()
    {
        using var testScope = host.CreateTestScope();
        var ownerId = await testScope.CreateUser();
        var organizationUserId = await testScope.CreateUser();

        var organizationUser = new OrganizationUser { UserId = organizationUserId, AccessLevel = AccessLevel.Read };
        var epic = new Epic { Name = "Epic 1", UserId = ownerId };
        var space = new Space { Name = "Space 1", CreatorId = ownerId, Epics = new List<Epic> { epic } };
        
        var organization = new Organization
        {
            OwnerId = ownerId,
            Users = new List<OrganizationUser> { organizationUser },
            Spaces = new List<Space> { space },
        };

        var spaceOrganizationUser = new SpaceOrganizationUser
        {
            OrganizationUser = organizationUser,
            AccessLevel = AccessLevel.Create,
        };
        
        var epicOrganizationUser = new EpicOrganizationUser
        {
            OrganizationUser = organizationUser,
            AccessLevel = AccessLevel.Delete,
            Epic = epic,
        };
        
        testScope.Database.Add(organization);
        testScope.Database.Add(spaceOrganizationUser);
        testScope.Database.Add(epicOrganizationUser);
        await testScope.Database.SaveChangesAsync();
        
        var permissions = await _organizationsController
            .WithAuthorization(ownerId)
            .Execute(x => x.GetPermissions(
                new GetPermissionsRequest
                {
                    OrganizationUserId = organizationUser.Id
                }));
        
        Assert.NotNull(permissions);
        Assert.Equal(AccessLevel.Read, permissions.OrganizationAccessLevel);
        
        Assert.NotNull(permissions.SpacesAccessLevels);
        Assert.Null(permissions.SpacesAccessLevels.DirectAccess);
        Assert.Equal(AccessLevel.Create, permissions.SpacesAccessLevels.AccessLevel);
        
        Assert.NotNull(permissions.EpicsAccessLevels);
        Assert.Equal(AccessLevel.None, permissions.EpicsAccessLevels.AccessLevel);
        Assert.NotNull(permissions.EpicsAccessLevels.DirectAccess);
        var directEpicAccess = Assert.Single(permissions.EpicsAccessLevels.DirectAccess);
        Assert.Equal(epic.Id, directEpicAccess.Key);
        Assert.Equal(AccessLevel.Delete, directEpicAccess.Value);
    }
}