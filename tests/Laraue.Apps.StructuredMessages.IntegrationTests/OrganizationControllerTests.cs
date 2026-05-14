using System.Net;
using Laraue.Apps.StructuredMessages.DataAccess.Enums;
using Laraue.Apps.StructuredMessages.DataAccess.Models;
using Laraue.Apps.StructuredMessages.IntegrationTests.Infrastructure;
using Laraue.Apps.StructuredMessages.Services;
using Laraue.Apps.StructuredMessages.WebApiHost.Controllers;
using Laraue.Apps.StructuredMessages.WebApiServices;
using Laraue.Core.Exceptions.Web;
using LinqToDB.EntityFrameworkCore;

namespace Laraue.Apps.StructuredMessages.IntegrationTests;

[Collection("IntegrationTest")]
public class OrganizationControllerTests(WebApiTestHost host) : IClassFixture<WebApiTestHost>
{
    private readonly Proxy<OrganizationsController> _organizationsController = host.Controller<OrganizationsController>();
    private readonly Proxy<SpacesController> _spacesController = host.Controller<SpacesController>();

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
        Assert.Equal(8, organization.JoinCode!.Length);
        Assert.True(organization.CreatedAt != default);
        Assert.True(organization.UpdatedAt != default);
        
        var organizationUsers = await testScope.Database.OrganizationUsers.ToListAsyncEF();
        var organizationUser = Assert.Single(organizationUsers);
        Assert.Equal(userId, organizationUser.UserId);
        Assert.Equal(organization.Id, organizationUser.OrganizationId);
        Assert.Equal(ChildrenAccessLevel.Read | ChildrenAccessLevel.Create | ChildrenAccessLevel.Update | ChildrenAccessLevel.Delete, organizationUser.SpacesAccessLevel);
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
        
        await testScope.InitializeOrganization(userId, org => org
            .WithName("Org 1")
            .WithColor("#ffffff")
            .AddUser(user2Id, builder => builder
                .SetSpacesAccessLevel(ChildrenAccessLevel.Create)
                .SetAdminAccessLevel(AdminAccessLevel.DeleteOrganization)));
        
        await testScope.InitializePersonalOrganization(user2Id, org => org
            .WithName("Org 2")
            .WithColor("#000000"));
        
        // First user see only owned organization
        var organizations = await _organizationsController
            .WithUserAuthorization(userId)
            .Execute(x => x.GetOrganizations());
        var organization = Assert.Single(organizations!);
        
        Assert.True(organization.CanCreateSpaces);
        Assert.True(organization.CanDelete);
        Assert.True(organization.CanUpdate);
        Assert.Equal("Org 1", organization.Name);
        Assert.Equal("#ffffff", organization.Color);
        
        // Second user see owned organization and organization where he was added
        organizations = await _organizationsController
            .WithUserAuthorization(user2Id)
            .Execute(x => x.GetOrganizations());
        
        Assert.Equal(2, organizations!.Length);
        var (personalOrganization, additionalOrganization) = (organizations[1], organizations[0]);
        
        Assert.False(personalOrganization.IsPersonal);
        Assert.True(personalOrganization.CanCreateSpaces);
        Assert.True(personalOrganization.CanDelete);
        Assert.False(personalOrganization.CanUpdate);
        Assert.Equal("Org 1", personalOrganization.Name);
        Assert.Equal("#ffffff", personalOrganization.Color);
        
        Assert.True(additionalOrganization.IsPersonal);
        Assert.True(additionalOrganization.CanCreateSpaces);
        Assert.False(additionalOrganization.CanDelete);
        Assert.True(additionalOrganization.CanUpdate);
        Assert.Equal("Org 2", additionalOrganization.Name);
        Assert.Equal("#000000", additionalOrganization.Color);
    }
    
    [Fact]
    public async Task User_ShouldNotViewUnavailableOrganizations_Always()
    {
        using var testScope = host.CreateTestScope();
        var userId = await testScope.CreateUser();
        var nonPermittedUserId = await testScope.CreateUser();

        await testScope.InitializeOrganization(userId);
        
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
        var entity = await testScope.InitializeOrganization(userId, org => org
            .WithName("Org 1")
            .WithColor("#ffffff")
            .WithTimestamp(date1));
        
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

        var entity = await testScope.InitializeOrganization(userId, org => org
            .AddUser(participatorId, builder => builder
                .SetAdminAccessLevel(AdminAccessLevel.UpdateOrganization)));
        
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

        var entity = await testScope.InitializeOrganization(userId, org => org
            .AddUser(participatorId, builder => builder
                .SetAdminAccessLevel(AdminAccessLevel.None)));
        
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

        var entity = await testScope.InitializeOrganization(userId);
        
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

        var entity = await testScope.InitializeOrganization(userId, org => org
            .AddUser(participatorId, builder => builder
                .SetAdminAccessLevel(AdminAccessLevel.DeleteOrganization)));
        
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

        var entity = await testScope.InitializeOrganization(userId, org => org
            .AddUser(participatorId, builder => builder
                .SetAdminAccessLevel(AdminAccessLevel.UpdateOrganization)));
        
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
            JoinCode = "abc",
            Color = "#ffffff"
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
            JoinCode = "abc",
            Color = "#ffffff"
        };
        
        testScope.Database.Organizations.Add(organization);
        await testScope.Database.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => _organizationsController
            .WithUserAuthorization(newUserId)
            .Execute(x => x.Join("def")));
        
        Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
    }
    
    [Fact]
    public async Task User_ShouldLeaveOrganization_WhenIsParticipator()
    {
        using var testScope = host.CreateTestScope();
        var ownerId = await testScope.CreateUser();
        var newUserId = await testScope.CreateUser();

        var organization = await testScope.InitializeOrganization(ownerId, org => org
            .AddUser(newUserId));
        
        // Ensure that user was in DB
        var organizationUsers = await testScope.Database.OrganizationUsers.ToListAsyncEF();
        Assert.NotNull(organizationUsers.FirstOrDefault(x => x.UserId == newUserId));
        
        await _organizationsController
            .WithUserAuthorization(newUserId)
            .Execute(x => x.Leave(organization.Id));
        
        // Ensure that now the record is missing
        organizationUsers = await testScope.Database.OrganizationUsers.ToListAsyncEF();
        Assert.Null(organizationUsers.FirstOrDefault(x => x.UserId == newUserId));
    }

    [Fact]
    public async Task User_ShouldSetEmployeeDirectAccessInOwnedOrganization_Always()
    {
        using var testScope = host.CreateTestScope();
        var ownerId = await testScope.CreateUser();
        var userIdToReceivePermissions = await testScope.CreateUser();

        var organization = await testScope.InitializeOrganization(ownerId, org => org
            .AddUser(userIdToReceivePermissions));
        
        var space = organization.Spaces![0];
        var epic = space.Epics![0];
        var organizationUser = organization.Users![1];

        var request = new SetPermissionsRequest
        {
            UserPermissions = new UserPermissions
            {
                Direct = new Dictionary<long, DirectSpaceAccessLevel>
                {
                    [space.Id] = new ()
                    {
                        DirectEpics = new Dictionary<long, DirectEpicAccessLevel>
                        {
                            [epic.Id] = new()
                            {
                                Issues = ChildrenAccessLevel.All,
                            },
                        }
                    }
                },
                Admin = AdminAccessLevel.Manage
            }
        };
        
        await _organizationsController
            .WithOrganizationAuthorization(organization.Id, ownerId)
            .Execute(x => x.SetUserPermissions(organizationUser.Id, request));
        
        organizationUser = await testScope.Database.OrganizationUsers.FirstAsyncEF(x => x.Id == organizationUser.Id);
        Assert.Equal(ChildrenAccessLevel.None, organizationUser.IssuesAccessLevel);
        Assert.Equal(ChildrenAccessLevel.None, organizationUser.EpicsAccessLevel);
        Assert.Equal(ChildrenAccessLevel.None, organizationUser.SpacesAccessLevel);
        Assert.Equal(AdminAccessLevel.Manage, organizationUser.AdminAccessLevel);
        
        var spaceOrganizationUser = await testScope.Database.DirectSpacePermissions.FirstAsyncEF(x => x.OrganizationUserId == organizationUser.Id);
        Assert.Equal(organizationUser.Id, spaceOrganizationUser.OrganizationUserId);
        Assert.Equal(space.Id, spaceOrganizationUser.SpaceId);
        Assert.Equal(EntityAccessLevel.Read, spaceOrganizationUser.EntityAccessLevel);
        Assert.Equal(ChildrenAccessLevel.None, spaceOrganizationUser.ChildrenEpicsAccessLevel);
        Assert.Equal(ChildrenAccessLevel.None, spaceOrganizationUser.ChildrenIssuesAccessLevel);
        
        var epicOrganizationUsers = await testScope.Database.DirectEpicPermissions.ToListAsyncEF();
        var epicOrganizationUser = Assert.Single(epicOrganizationUsers);
        Assert.Equal(organizationUser.Id, epicOrganizationUser.OrganizationUserId);
        Assert.Equal(epic.Id, epicOrganizationUser.EpicId);
        Assert.Equal(EntityAccessLevel.Read, epicOrganizationUser.EntityAccessLevel);
        Assert.Equal(ChildrenAccessLevel.All, epicOrganizationUser.ChildrenIssuesAccessLevel);
    }
    
    [Fact]
    public async Task User_ShouldNotSetDirectAccessInOwnedOrganization_WhenDataIsIncorrect()
    {
        using var testScope = host.CreateTestScope();
        var ownerId = await testScope.CreateUser();
        var userIdToReceivePermissions = await testScope.CreateUser();

        var organization = await testScope.InitializeOrganization(ownerId, org => org
            .AddUser(userIdToReceivePermissions));
        
        var space = organization.GetSpace(0);
        var epic = organization.GetEpic(0, 0);
        var organizationUser = organization.Users![1];

        var request = new SetPermissionsRequest
        {
            UserPermissions = new UserPermissions
            {
                Direct = new Dictionary<long, DirectSpaceAccessLevel>
                {
                    [space.Id] = new ()
                    {
                        DirectEpics = new Dictionary<long, DirectEpicAccessLevel>
                        {
                            [0] = new() // Unexists epic
                            {
                                Issues = ChildrenAccessLevel.All,
                            },
                            [epic.Id] = new ()
                            {
                                Self = EntityAccessLevel.All, // Attempt to set delete permission for default epic
                            }
                        },
                        Self = EntityAccessLevel.All, // Attempt to set delete permission for default space
                    },
                    [0] = new () // Unexists space
                    {
                        Epics = ChildrenAccessLevel.All
                    }
                },
                Admin = AdminAccessLevel.Manage
            }
        };
        
        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => _organizationsController
            .WithOrganizationAuthorization(organization.Id, ownerId)
            .Execute(x => x.SetUserPermissions(organizationUser.Id, request)));
        var badRequest = ex.HasInnerException<BadRequestException>();
        var exceptedErrors = new[]
        {
            $"Space: '{space.Id}'. Attempt to add delete permission to Default space",
            $"Space: '{space.Id}', Epic: '0'. Entity is not found",
            $"Space: '{space.Id}', Epic: '{epic.Id}'. Attempt to add delete permission to Default epic",
            "Space: '0'. Entity is not found",
        };
        Assert.Equal(exceptedErrors, badRequest.Errors["direct"]);
    }
    
    [Fact]
    public async Task User_ShouldSetEmployeeSectionAccess_WhenHasAccess()
    {
        using var testScope = host.CreateTestScope();
        var ownerId = await testScope.CreateUser();
        var adminId = await testScope.CreateUser();
        var userIdToReceivePermissions = await testScope.CreateUser();
        
        var organization = await testScope.InitializeOrganization(ownerId, org => org
            .AddUser(adminId, builder => builder
                .SetAdminAccessLevel(AdminAccessLevel.Manage))
            .AddUser(userIdToReceivePermissions));
        
        var organizationUser = organization.Users![2];

        var request = new SetPermissionsRequest
        {
            UserPermissions = new UserPermissions()
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
        
        var organization = await testScope.InitializeOrganization(ownerId, org => org
            .AddUser(nonPermittedUserId, builder => builder.SetAdminAccessLevel(AdminAccessLevel.Manage))
            .AddUser(userIdToReceivePermissions));
        
        var organizationUser = organization.Users![2];

        var request = new SetPermissionsRequest
        {
            UserPermissions = new UserPermissions()
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

        var organization = await testScope.InitializeOrganization(ownerId, org => org
            .AddUser(organizationUserId));

        var organizationUser = organization.Users![1];
        var permissions = await _organizationsController
            .WithOrganizationAuthorization(organization.Id, ownerId)
            .Execute(x => x.GetUserPermissions(organizationUser.Id));

        Assert.NotNull(permissions);
        Assert.Equal(ChildrenAccessLevel.None, permissions.Global.Epics);
        Assert.Equal(ChildrenAccessLevel.None, permissions.Global.Issues);
        Assert.Equal(ChildrenAccessLevel.None, permissions.Global.Spaces);
        Assert.Equal(AdminAccessLevel.None, permissions.Admin);
        Assert.Empty(permissions.Direct);
    }

    [Fact]
    public async Task User_ShouldViewPermissionsOfNewEmployee_WhenHasAccess()
    {
        using var testScope = host.CreateTestScope();
        var ownerId = await testScope.CreateUser();
        var adminId = await testScope.CreateUser();
        var organizationUserId = await testScope.CreateUser();

        var organization = await testScope.InitializeOrganization(ownerId, org => org
            .AddUser(adminId, builder => builder.SetAdminAccessLevel(AdminAccessLevel.Manage))
            .AddUser(organizationUserId));

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

        var organization = await testScope.InitializeOrganization(ownerId, org => org
            .AddUser(nonPermittedUserId, builder => builder.SetAdminAccessLevel(AdminAccessLevel.None))
            .AddUser(organizationUserId));

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

        var organization = await testScope.InitializeOrganization(ownerId, org => org
            .AddUser(organizationUserId, builder => builder
                .SetSpacesAccessLevel(ChildrenAccessLevel.Create)
                .SetDefaultSpaceBacklogAccessLevel(EntityAccessLevel.Delete)));

        var organizationUser = organization.Users![1];
        var space = organization.Spaces![0];
        var epic = space.Epics![0];

        var permissions = await _organizationsController
            .WithOrganizationAuthorization(organization.Id, ownerId)
            .Execute(x => x.GetUserPermissions(organizationUser.Id));

        Assert.NotNull(permissions);
        Assert.Equal(ChildrenAccessLevel.None, permissions.Global.Epics);
        Assert.Equal(ChildrenAccessLevel.None, permissions.Global.Issues);
        Assert.Equal(ChildrenAccessLevel.Read | ChildrenAccessLevel.Create, permissions.Global.Spaces);
        Assert.Equal(AdminAccessLevel.None, permissions.Admin);
        
        var directSpaceAccess = Assert.Single(permissions.Direct);
        Assert.Equal(space.Id, directSpaceAccess.Key);
        Assert.Equal(ChildrenAccessLevel.None, directSpaceAccess.Value.Epics);
        Assert.Equal(ChildrenAccessLevel.None, directSpaceAccess.Value.Issues);
        Assert.Equal(EntityAccessLevel.Read, directSpaceAccess.Value.Self);
        
        var directEpicsAccess = Assert.Single(directSpaceAccess.Value.DirectEpics);
        Assert.Equal(epic.Id, directEpicsAccess.Key);
        Assert.Equal(ChildrenAccessLevel.None, directEpicsAccess.Value.Issues);
        Assert.Equal(EntityAccessLevel.Read | EntityAccessLevel.Delete, directEpicsAccess.Value.Self);
    }

    [Fact]
    public async Task User_ShouldRevokeAccess_WhenHasPermissionManagementPermission()
    {
        using var testScope = host.CreateTestScope();
        var ownerId = await testScope.CreateUser();
        var adminId = await testScope.CreateUser();
        var participatorId = await testScope.CreateUser();

        var organization = await testScope.InitializeOrganization(ownerId, org => org
            .AddUser(adminId, builder => builder.SetAdminAccessLevel(AdminAccessLevel.Manage))
            .AddUser(participatorId));

        var user = organization.Users![2];
        
        // Ensure that record exists
        var organizationUsers = await testScope.Database.OrganizationUsers.ToListAsyncEF();
        Assert.NotNull(organizationUsers.FirstOrDefault(x => x.UserId == participatorId));
        
        await _organizationsController
            .WithOrganizationAuthorization(organization.Id, adminId)
            .Execute(x => x.RevokeAccess(user.Id));
        
        // Ensure that now the record is missing
        organizationUsers = await testScope.Database.OrganizationUsers.ToListAsyncEF();
        Assert.Null(organizationUsers.FirstOrDefault(x => x.UserId == participatorId));
    }

    [Fact]
    public async Task User_ShouldRegenerateJoinCode_WhenHasManagePermission()
    {
        using var testScope = host.CreateTestScope();
        var ownerId = await testScope.CreateUser();
        var adminId = await testScope.CreateUser();

        var organization = await testScope.InitializeOrganization(ownerId, org => org
            .AddUser(adminId, builder => builder.SetAdminAccessLevel(AdminAccessLevel.Manage)));
        
        var newCode = await _organizationsController
            .WithOrganizationAuthorization(organization.Id, adminId)
            .Execute(x => x.RegenerateCode());
        
        Assert.Equal(8, newCode!.Length);
        organization = await testScope.Database.Organizations.SingleAsyncEF();
        Assert.Equal(newCode, organization.JoinCode);
    }

    [Fact]
    public async Task Owner_ShouldRegenerateJoinCode_Always()
    {
        using var testScope = host.CreateTestScope();
        var ownerId = await testScope.CreateUser();

        var organization = await testScope.InitializeOrganization(ownerId);
        
        var newCode = await _organizationsController
            .WithOrganizationAuthorization(organization.Id, ownerId)
            .Execute(x => x.RegenerateCode());
        
        Assert.Equal(8, newCode!.Length);
        organization = await testScope.Database.Organizations.SingleAsyncEF();
        Assert.Equal(newCode, organization.JoinCode);
    }
    
    [Fact]
    public async Task Login_ShouldReturnTokenForOwnedOrganization_Always()
    {
        using var testScope = host.CreateTestScope();
        var ownerId = await testScope.CreateUser();

        var organization = await testScope.InitializeOrganization(ownerId);
        
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

        var organization = await testScope.InitializeOrganization(ownerId, org => org
            .AddUser(participatorId));
        
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

        var organization = await testScope.InitializeOrganization(ownerId);
        
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