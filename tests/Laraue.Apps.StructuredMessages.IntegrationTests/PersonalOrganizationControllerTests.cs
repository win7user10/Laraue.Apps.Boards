using System.Net;
using Laraue.Apps.StructuredMessages.IntegrationTests.Infrastructure;
using Laraue.Apps.StructuredMessages.WebApiHost.Controllers;
using Laraue.Apps.StructuredMessages.WebApiServices;
using Laraue.Core.Exceptions.Web;
using LinqToDB.EntityFrameworkCore;

namespace Laraue.Apps.StructuredMessages.IntegrationTests;

[Collection("IntegrationTest")]
public class PersonalOrganizationControllerTests(WebApiTestHost host) : IClassFixture<WebApiTestHost>
{
    private readonly Proxy<OrganizationsController> _organizationsController = host.Controller<OrganizationsController>();
    private readonly Proxy<SpacesController> _spacesController = host.Controller<SpacesController>();
    
    [Fact]
    public async Task User_ShouldUpdatePersonalOrganization_Always()
    {
        using var testScope = host.CreateTestScope();
        
        var timestamp = DateTime.UtcNow;
        var userId = await testScope.CreateUser();
        var organization = await testScope.InitializePersonalOrganization(
            userId,
            setup => setup
                .WithTimestamp(timestamp));

        await _organizationsController
            .WithUserAuthorization(userId)
            .Execute(x => x.Update(
                organization.Id,
                new EditOrganizationRequest
                {
                    Name = "Org 2",
                    Color = "#000000"
                }));

        var organizations = await testScope.Database.Organizations.ToListAsyncEF();
        
        organization = Assert.Single(organizations);
        Assert.Equal("Org 2", organization.Name);
        Assert.Equal("#000000", organization.Color);
        Assert.Equal(timestamp, organization.CreatedAt, new TimeSpan(10));
        Assert.True(organization.UpdatedAt > timestamp);
    }
    
    [Fact]
    public async Task User_ShouldNotUpdateSomeonePersonalOrganization_Always()
    {
        using var testScope = host.CreateTestScope();
        var userId = await testScope.CreateUser();
        var nonPermittedUserId = await testScope.CreateUser();

        var organization = await testScope.InitializePersonalOrganization(userId);
        
        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => _organizationsController
            .WithUserAuthorization(nonPermittedUserId)
            .Execute(x => x.Update(
                organization.Id,
                new EditOrganizationRequest
                {
                    Name = "Org 2",
                    Color = "#000000"
                })));
        
        Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
    }
    
    [Fact]
    public async Task User_ShouldNotDeletePersonalOrganization_Always()
    {
        using var testScope = host.CreateTestScope();
        var userId = await testScope.CreateUser();
        var organization = await testScope.InitializePersonalOrganization(userId);
        
        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => _organizationsController
            .WithUserAuthorization(userId)
            .Execute(x => x.Delete(organization.Id)));

        var forbidden = exception.HasInnerException<NotFoundException>();
        Assert.Equal($"Organization: {organization.Id} is unavailable or permission: DeleteOrganization is missing", forbidden.Message);
    }
    
    [Fact]
    public async Task User_ShouldNotDeleteSomeonePersonalOrganization_Always()
    {
        using var testScope = host.CreateTestScope();
        var userId = await testScope.CreateUser();
        var nonPermittedUserId = await testScope.CreateUser();
        var organization = await testScope.InitializePersonalOrganization(userId);
        
        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => _organizationsController
            .WithUserAuthorization(nonPermittedUserId)
            .Execute(x => x.Delete(organization.Id)));
        
        Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
    }
    
    [Fact]
    public async Task Login_ShouldReturnValidOrganizationToken_Always()
    {
        using var testScope = host.CreateTestScope();
        var ownerId = await testScope.CreateUser();
        var organization = await testScope.InitializePersonalOrganization(ownerId);
        
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
    public async Task User_ShouldViewOrganization_Always()
    {
        using var testScope = host.CreateTestScope();
        var ownerId = await testScope.CreateUser();
        var organization = await testScope.InitializePersonalOrganization(ownerId);
        
        var result = await _organizationsController
            .WithOrganizationAuthorization(organization.Id, ownerId)
            .Execute(x => x.GetOrganization());
        
        Assert.NotNull(result);
        Assert.False(result.CanManage);
        Assert.True(result.CanMassMove);
        Assert.True(result.CanCreateSpaces);
    }
}