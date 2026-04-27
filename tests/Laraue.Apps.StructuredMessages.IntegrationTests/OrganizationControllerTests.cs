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
    private readonly Proxy<PersonalSpacesController> _spacesController = host.Controller<PersonalSpacesController>();

    [Fact]
    public async Task CreateOrganization_ShouldCreateNewOrganizationWithDefaults_Always()
    {
        using var testScope = host.CreateTestScope();
        var userId = await testScope.CreateUser();
        
        var newId = await _organizationsController
            .WithUserAuthorization(userId)
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
        
        var organizationUsers = await testScope.Database.OrganizationUsers.ToListAsyncEF();
        var organizationUser = Assert.Single(organizationUsers);
        Assert.Equal(userId, organizationUser.UserId);
        Assert.Equal(organization.Id, organizationUser.OrganizationId);
        Assert.Equal(ItemsAccessLevel.Read | ItemsAccessLevel.Create | ItemsAccessLevel.Update | ItemsAccessLevel.Delete, organizationUser.ItemsAccessLevel);
        Assert.Equal(AdminAccessLevel.All, organizationUser.AdminAccessLevel);
        
        var spaces = await testScope.Database.Spaces.ToListAsyncEF();
        var space = Assert.Single(spaces);
        Assert.Equal("Default Space", space.Name);
        Assert.False(string.IsNullOrEmpty(space.Color));
        Assert.Equal(userId, space.CreatorId);
        Assert.Equal(organization.Id, space.OrganizationId);
        Assert.True(space.CreatedAt != default);
        Assert.True(space.UpdatedAt != default);
        Assert.True(space.IsDefault);
        
        var epics = await testScope.Database.Epics.ToListAsyncEF();
        var epic = Assert.Single(epics);
        Assert.Equal("Backlog", epic.Name);
        Assert.False(string.IsNullOrEmpty(epic.Color));
        Assert.Equal(userId, epic.UserId);
        Assert.Equal(space.Id, epic.SpaceId);
        Assert.True(epic.CreatedAt != default);
        Assert.True(epic.UpdatedAt != default);
        Assert.True(epic.TouchedAt != default);
        Assert.True(epic.IsDefault);
    }
    
    [Fact]
    public async Task User_ShouldViewOwnedAndParticipatingOrganizations_Always()
    {
        using var testScope = host.CreateTestScope();
        var userId = await testScope.CreateUser();
        var user2Id = await testScope.CreateUser();
        
        await new OrganizationInitializer(testScope.Database, userId)
            .WithName("Org 1")
            .WithColor("#ffffff")
            .AddUser(user2Id, builder => builder
                .SetOrganizationAccessLevel(ItemsAccessLevel.Update))
            .Initialize();
        
        await new OrganizationInitializer(testScope.Database, user2Id)
            .WithName("Org 2")
            .WithColor("#000000")
            .Initialize();
        
        // First user see only owned organization
        var organizations = await _organizationsController
            .WithUserAuthorization(userId)
            .Execute(x => x.GetOrganizations());
        var organization = Assert.Single(organizations!);
        
        Assert.Equal(ItemsAccessLevel.All, organization.ItemsAccessLevel);
        Assert.Equal("Org 1", organization.Name);
        Assert.Equal("#ffffff", organization.Color);
        Assert.Equal(1, organization.SpacesCount);
        
        // Second user see owned organization and organization where he was added
        organizations = await _organizationsController
            .WithUserAuthorization(user2Id)
            .Execute(x => x.GetOrganizations());
        
        Assert.Equal(2, organizations!.Length);
        var (organization1, organization2) = (organizations[0], organizations[1]);
        
        Assert.Equal(ItemsAccessLevel.Update, organization1.ItemsAccessLevel);
        Assert.Equal("Org 1", organization1.Name);
        Assert.Equal("#ffffff", organization1.Color);
        Assert.Equal(1, organization1.SpacesCount);
        
        Assert.Equal(ItemsAccessLevel.All, organization2.ItemsAccessLevel);
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

        await new OrganizationInitializer(testScope.Database, userId)
            .Initialize();
        
        await testScope.Database.SaveChangesAsync();
        
        var organizations = await _organizationsController
            .WithUserAuthorization(nonPermittedUserId)
            .Execute(x => x.GetOrganizations());
        
        Assert.Empty(organizations!);
    }
    
    [Fact]
    public async Task User_ShouldUpdateOwnedOrganization_Always()
    {
        using var testScope = host.CreateTestScope();
        var userId = await testScope.CreateUser();

        var date1 = new DateTime(2020, 01, 01, 0, 0, 0, DateTimeKind.Utc);
        var entity = await new OrganizationInitializer(testScope.Database, userId)
            .WithName("Org 1")
            .WithColor("#ffffff")
            .WithTimestamp(date1)
            .Initialize();
        
        await _organizationsController
            .WithUserAuthorization(userId)
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
    public async Task User_ShouldUpdateOrganization_WhenHasAccess()
    {
        using var testScope = host.CreateTestScope();
        var userId = await testScope.CreateUser();
        var participatorId = await testScope.CreateUser();

        var entity = await new OrganizationInitializer(testScope.Database, userId)
            .AddUser(participatorId, builder => builder.SetOrganizationAccessLevel(AdminAccessLevel.UpdateOrganization))
            .Initialize();
        
        await _organizationsController
            .WithUserAuthorization(participatorId)
            .Execute(x => x.Update(
                entity.Id,
                new EditOrganizationRequest
                {
                    Name = "Org 2",
                    Color = "#000000"
                }));
    }
    
    [Fact]
    public async Task User_ShouldNotUpdateOrganization_WhenHasNoAccess()
    {
        using var testScope = host.CreateTestScope();
        var userId = await testScope.CreateUser();
        var participatorId = await testScope.CreateUser();

        var entity = await new OrganizationInitializer(testScope.Database, userId)
            .AddUser(participatorId, builder => builder.SetOrganizationAccessLevel(ItemsAccessLevel.Read))
            .Initialize();
        
        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => _organizationsController
            .WithUserAuthorization(participatorId)
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
    public async Task User_ShouldDeleteOwnedOrganization_Always()
    {
        using var testScope = host.CreateTestScope();
        var userId = await testScope.CreateUser();

        var entity = await new OrganizationInitializer(testScope.Database, userId)
            .Initialize();
        
        await _organizationsController
            .WithUserAuthorization(userId)
            .Execute(x => x.Delete(entity.Id));

        var organizations = await testScope.Database.Organizations.ToListAsyncEF();
        Assert.Empty(organizations);
    }
    
    [Fact]
    public async Task User_ShouldDeleteOrganization_WhenHasAccess()
    {
        using var testScope = host.CreateTestScope();
        var userId = await testScope.CreateUser();
        var participatorId = await testScope.CreateUser();

        var entity = await new OrganizationInitializer(testScope.Database, userId)
            .AddUser(participatorId, builder => builder.SetOrganizationAccessLevel(AdminAccessLevel.DeleteOrganization))
            .Initialize();
        
        await _organizationsController
            .WithUserAuthorization(userId)
            .Execute(x => x.Delete(entity.Id));
        
        var organizations = await testScope.Database.Organizations.ToListAsyncEF();
        Assert.Empty(organizations);
    }
    
    [Fact]
    public async Task User_ShouldNotDeleteOrganization_WhenHasNotAccess()
    {
        using var testScope = host.CreateTestScope();
        var userId = await testScope.CreateUser();
        var participatorId = await testScope.CreateUser();

        var entity = await new OrganizationInitializer(testScope.Database, userId)
            .AddUser(participatorId, builder => builder.SetOrganizationAccessLevel(AdminAccessLevel.UpdateOrganization))
            .Initialize();
        
        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => _organizationsController
            .WithUserAuthorization(participatorId)
            .Execute(x => x.Delete(entity.Id)));
        
        Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
    }
    
    [Fact]
    public async Task User_ShouldJoinOrganization_WhenIsNotOrganizationMember()
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
            .WithUserAuthorization(newUserId)
            .Execute(x => x.Join("abc"));
        
        var organizationUsers = await testScope.Database.OrganizationUsers.ToListAsyncEF();
        var organizationUser = Assert.Single(organizationUsers);
        
        Assert.Equal(organization.Id, organizationUser.OrganizationId);
        Assert.Equal(newUserId, organizationUser.UserId);
        
        var exception = await Assert.ThrowsAsync<HttpRequestException>(() =>  _organizationsController
            .WithUserAuthorization(newUserId)
            .Execute(x => x.Join("abc")));
        
        Assert.Equal(HttpStatusCode.NotAcceptable, exception.StatusCode);
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
            .WithUserAuthorization(newUserId)
            .Execute(x => x.Join("def")));
        
        Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
    }

    [Fact]
    public async Task User_ShouldSetEmployeeDirectAccessInOwnedOrganization_Always()
    {
        using var testScope = host.CreateTestScope();
        var ownerId = await testScope.CreateUser();
        var userIdToReceivePermissions = await testScope.CreateUser();

        var organization = await new OrganizationInitializer(testScope.Database, ownerId)
            .AddUser(userIdToReceivePermissions)
            .Initialize();
        
        var space = organization.Spaces![0];
        var epic = space.Epics![0];
        var organizationUser = organization.Users![1];

        var request = new SetPermissionsRequest
        {
            UserPermissions = new UserPermissions
            {
                OrganizationItemsAccessLevel = ItemsAccessLevel.Read,
                EpicsAccessLevels = new AccessLevels
                {
                    DirectAccess = new Dictionary<long, ItemsAccessLevel>
                    {
                        [epic.Id] = ItemsAccessLevel.Create,
                    }
                },
                SpacesAccessLevels = new AccessLevels
                {
                    DirectAccess = new Dictionary<long, ItemsAccessLevel>
                    {
                        [space.Id] = ItemsAccessLevel.Update,
                    }
                }
            }
        };
        
        await _organizationsController
            .WithOrganizationAuthorization(organization.Id, ownerId)
            .Execute(x => x.SetUserPermissions(organizationUser.Id, request));
        
        organizationUser = await testScope.Database.OrganizationUsers.FirstAsyncEF(x => x.Id == organizationUser.Id);
        Assert.Equal(ItemsAccessLevel.Read, organizationUser.ItemsAccessLevel);
        
        var spaceOrganizationUser = await testScope.Database.SpaceOrganizationUsers.FirstAsyncEF(x => x.OrganizationUserId == organizationUser.Id);
        Assert.Equal(ItemsAccessLevel.Update, spaceOrganizationUser.ItemsAccessLevel);
        Assert.Equal(organizationUser.Id, spaceOrganizationUser.OrganizationUserId);
        Assert.Equal(space.Id, spaceOrganizationUser.SpaceId);
        
        var epicOrganizationUsers = await testScope.Database.EpicOrganizationUsers.ToListAsyncEF();
        var epicOrganizationUser = Assert.Single(epicOrganizationUsers);
        Assert.Equal(ItemsAccessLevel.Create, epicOrganizationUser.ItemsAccessLevel);
        Assert.Equal(organizationUser.Id, epicOrganizationUser.OrganizationUserId);
        Assert.Equal(epic.Id, epicOrganizationUser.EpicId);
    }
    
    [Fact]
    public async Task User_ShouldSetEmployeeSectionAccessInOwnedOrganization_Always()
    {
        using var testScope = host.CreateTestScope();
        var ownerId = await testScope.CreateUser();
        var userIdToReceivePermissions = await testScope.CreateUser();
        
        var organization = await new OrganizationInitializer(testScope.Database, ownerId)
            .AddUser(userIdToReceivePermissions)
            .Initialize();
        
        var organizationUser = organization.Users![1];

        var request = new SetPermissionsRequest
        {
            UserPermissions = new UserPermissions
            {
                OrganizationItemsAccessLevel = ItemsAccessLevel.None,
                EpicsAccessLevels = new AccessLevels
                {
                    ItemsAccessLevel = ItemsAccessLevel.Delete,
                },
                SpacesAccessLevels = new AccessLevels
                {
                    ItemsAccessLevel = ItemsAccessLevel.Read,
                }
            }
        };
        
        await _organizationsController
            .WithOrganizationAuthorization(organization.Id, ownerId)
            .Execute(x => x.SetUserPermissions(organizationUser.Id, request));
        
        organizationUser = await testScope.Database.OrganizationUsers.FirstAsyncEF(x => x.Id == organizationUser.Id);
        Assert.Equal(ItemsAccessLevel.None, organizationUser.ItemsAccessLevel);
        
        var spaceOrganizationUser = await testScope.Database.SpaceOrganizationUsers.FirstAsyncEF(x => x.OrganizationUserId == organizationUser.Id);
        Assert.Equal(ItemsAccessLevel.Read, spaceOrganizationUser.ItemsAccessLevel);
        Assert.Equal(organizationUser.Id, spaceOrganizationUser.OrganizationUserId);
        Assert.Null(spaceOrganizationUser.SpaceId);
        
        var epicOrganizationUsers = await testScope.Database.EpicOrganizationUsers.ToListAsyncEF();
        var epicOrganizationUser = Assert.Single(epicOrganizationUsers);
        Assert.Equal(ItemsAccessLevel.Delete, epicOrganizationUser.ItemsAccessLevel);
        Assert.Equal(organizationUser.Id, epicOrganizationUser.OrganizationUserId);
        Assert.Null(epicOrganizationUser.EpicId);
    }
    
    [Fact]
    public async Task User_ShouldSetEmployeeSectionAccess_WhenHasAccess()
    {
        using var testScope = host.CreateTestScope();
        var ownerId = await testScope.CreateUser();
        var adminId = await testScope.CreateUser();
        var userIdToReceivePermissions = await testScope.CreateUser();
        
        var organization = await new OrganizationInitializer(testScope.Database, ownerId)
            .AddUser(adminId, builder => builder.SetOrganizationAccessLevel(AdminAccessLevel.ManagePermissions))
            .AddUser(userIdToReceivePermissions)
            .Initialize();
        
        var organizationUser = organization.Users![2];

        var request = new SetPermissionsRequest
        {
            UserPermissions = new UserPermissions
            {
                OrganizationItemsAccessLevel = ItemsAccessLevel.None,
                EpicsAccessLevels = new AccessLevels
                {
                    ItemsAccessLevel = ItemsAccessLevel.Delete,
                },
                SpacesAccessLevels = new AccessLevels
                {
                    ItemsAccessLevel = ItemsAccessLevel.Read,
                }
            }
        };
        
        await _organizationsController
            .WithOrganizationAuthorization(organization.Id, adminId)
            .Execute(x => x.SetUserPermissions(organizationUser.Id, request));
    }
    
    [Fact]
    public async Task User_ShouldNotSetEmployeeAccess_WhenHasNoAccess()
    {
        using var testScope = host.CreateTestScope();
        var ownerId = await testScope.CreateUser();
        var nonPermittedUserId = await testScope.CreateUser();
        var userIdToReceivePermissions = await testScope.CreateUser();
        
        var organization = await new OrganizationInitializer(testScope.Database, ownerId)
            .AddUser(nonPermittedUserId, builder => builder.SetOrganizationAccessLevel(ItemsAccessLevel.Create))
            .AddUser(userIdToReceivePermissions)
            .Initialize();
        
        var organizationUser = organization.Users![2];

        var request = new SetPermissionsRequest
        {
            UserPermissions = new UserPermissions
            {
                OrganizationItemsAccessLevel = ItemsAccessLevel.None,
                EpicsAccessLevels = new AccessLevels
                {
                    ItemsAccessLevel = ItemsAccessLevel.Delete,
                },
                SpacesAccessLevels = new AccessLevels
                {
                    ItemsAccessLevel = ItemsAccessLevel.Read,
                }
            }
        };
        
        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => _organizationsController
            .WithOrganizationAuthorization(organization.Id, userIdToReceivePermissions)
            .Execute(x => x.SetUserPermissions(organizationUser.Id, request)));
        Assert.Equal(HttpStatusCode.NotFound, ex.StatusCode);
    }
    
    [Fact]
    public async Task User_ShouldViewPermissionsOfNewEmployeeInOwnedOrganization_Always()
    {
        using var testScope = host.CreateTestScope();
        var ownerId = await testScope.CreateUser();
        var organizationUserId = await testScope.CreateUser();
        
        var organization = await new OrganizationInitializer(testScope.Database, ownerId)
            .AddUser(organizationUserId)
            .Initialize();

        var organizationUser = organization.Users![1];
        var permissions = await _organizationsController
            .WithOrganizationAuthorization(organization.Id, ownerId)
            .Execute(x => x.GetUserPermissions(organizationUser.Id));
        
        Assert.NotNull(permissions);
        Assert.Equal(ItemsAccessLevel.None, permissions.OrganizationItemsAccessLevel);
        
        Assert.NotNull(permissions.SpacesAccessLevels);
        Assert.Null(permissions.SpacesAccessLevels.DirectAccess);
        Assert.Equal(ItemsAccessLevel.None, permissions.SpacesAccessLevels.ItemsAccessLevel);
        
        Assert.NotNull(permissions.EpicsAccessLevels);
        Assert.Null(permissions.EpicsAccessLevels.DirectAccess);
        Assert.Equal(ItemsAccessLevel.None, permissions.EpicsAccessLevels.ItemsAccessLevel);
    }
    
    [Fact]
    public async Task User_ShouldViewPermissionsOfNewEmployee_WhenHasAccess()
    {
        using var testScope = host.CreateTestScope();
        var ownerId = await testScope.CreateUser();
        var adminId = await testScope.CreateUser();
        var organizationUserId = await testScope.CreateUser();
        
        var organization = await new OrganizationInitializer(testScope.Database, ownerId)
            .AddUser(adminId, builder => builder.SetOrganizationAccessLevel(AdminAccessLevel.ManagePermissions))
            .AddUser(organizationUserId)
            .Initialize();

        var organizationUser = organization.Users![2];
        var permissions = await _organizationsController
            .WithOrganizationAuthorization(organization.Id, ownerId)
            .Execute(x => x.GetUserPermissions(organizationUser.Id));
        
        Assert.NotNull(permissions);
    }
    
    [Fact]
    public async Task User_ShouldNotViewPermissionsOfNewEmployee_WhenHasNotAccess()
    {
        using var testScope = host.CreateTestScope();
        var ownerId = await testScope.CreateUser();
        var nonPermittedUserId = await testScope.CreateUser();
        var organizationUserId = await testScope.CreateUser();
        
        var organization = await new OrganizationInitializer(testScope.Database, ownerId)
            .AddUser(nonPermittedUserId, builder => builder.SetOrganizationAccessLevel(ItemsAccessLevel.Read))
            .AddUser(organizationUserId)
            .Initialize();

        var organizationUser = organization.Users![2];
        
        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => _organizationsController
            .WithOrganizationAuthorization(organization.Id, nonPermittedUserId)
            .Execute(x => x.GetUserPermissions(organizationUser.Id)));
        Assert.Equal(HttpStatusCode.NotFound, ex.StatusCode);
    }
    
    [Fact]
    public async Task User_ShouldViewEmployeePermissionsInOwnedOrganization_Always()
    {
        using var testScope = host.CreateTestScope();
        var ownerId = await testScope.CreateUser();
        var organizationUserId = await testScope.CreateUser();

        var organization = await new OrganizationInitializer(testScope.Database, ownerId)
            .AddUser(organizationUserId, builder => builder
                .SetOrganizationAccessLevel(ItemsAccessLevel.Read)
                .SetSpacesAccessLevel(ItemsAccessLevel.Create)
                .SetDefaultSpaceBacklogAccessLevel(ItemsAccessLevel.Delete))
            .Initialize();
        
        var organizationUser = organization.Users![1];
        var space = organization.Spaces![0];
        var epic = space.Epics![0];
        
        var permissions = await _organizationsController
            .WithOrganizationAuthorization(organization.Id, ownerId)
            .Execute(x => x.GetUserPermissions(organizationUser.Id));
        
        Assert.NotNull(permissions);
        Assert.Equal(ItemsAccessLevel.Read, permissions.OrganizationItemsAccessLevel);
        
        Assert.NotNull(permissions.SpacesAccessLevels);
        Assert.Null(permissions.SpacesAccessLevels.DirectAccess);
        Assert.Equal(ItemsAccessLevel.Create, permissions.SpacesAccessLevels.ItemsAccessLevel);
        
        Assert.NotNull(permissions.EpicsAccessLevels);
        Assert.Equal(ItemsAccessLevel.None, permissions.EpicsAccessLevels.ItemsAccessLevel);
        Assert.NotNull(permissions.EpicsAccessLevels.DirectAccess);
        var directEpicAccess = Assert.Single(permissions.EpicsAccessLevels.DirectAccess);
        Assert.Equal(epic.Id, directEpicAccess.Key);
        Assert.Equal(ItemsAccessLevel.Delete, directEpicAccess.Value);
    }
    
    [Fact]
    public async Task Login_ShouldReturnTokenForOwnedOrganization_Always()
    {
        using var testScope = host.CreateTestScope();
        var ownerId = await testScope.CreateUser();

        var organization = await new OrganizationInitializer(testScope.Database, ownerId).Initialize();
        
        var organizationAuthToken = await _organizationsController
            .WithUserAuthorization(ownerId)
            .Execute(x => x.Login(
                new LoginRequest
                {
                    OrganizationId = organization.Id
                }));
        
        Assert.NotNull(organizationAuthToken);
        
        // Spaces are available with organization token
        var getSpacesResponse = await _spacesController
            .WithAuthorizationToken(organizationAuthToken)
            .Execute(x => x.GetAll());
        
        Assert.NotNull(getSpacesResponse);
    }
    
    [Fact]
    public async Task Login_ShouldReturnTokenForOrganization_WhenHasAccess()
    {
        using var testScope = host.CreateTestScope();
        var ownerId = await testScope.CreateUser();
        var participatorId = await testScope.CreateUser();

        var organization = await new OrganizationInitializer(testScope.Database, ownerId)
            .AddUser(participatorId, builder => builder.SetOrganizationAccessLevel(ItemsAccessLevel.Read))
            .Initialize();
        
        var organizationAuthToken = await _organizationsController
            .WithUserAuthorization(participatorId)
            .Execute(x => x.Login(
                new LoginRequest
                {
                    OrganizationId = organization.Id
                }));
        
        Assert.NotNull(organizationAuthToken);
        
        // Spaces are available with organization token
        var getSpacesResponse = await _spacesController
            .WithAuthorizationToken(organizationAuthToken)
            .Execute(x => x.GetAll());
        
        Assert.NotNull(getSpacesResponse);
    }
    
    [Fact]
    public async Task Login_ShouldNotReturnTokenForOrganization_WhenHasNotAccess()
    {
        using var testScope = host.CreateTestScope();
        var ownerId = await testScope.CreateUser();
        var participatorId = await testScope.CreateUser();

        var organization = await new OrganizationInitializer(testScope.Database, ownerId)
            .Initialize();
        
        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => _organizationsController
            .WithUserAuthorization(participatorId)
            .Execute(x => x.Login(
                new LoginRequest
                {
                    OrganizationId = organization.Id
                })));
        Assert.Equal(HttpStatusCode.NotFound, ex.StatusCode);
    }
}