using Laraue.Apps.StructuredMessages.IntegrationTests.Infrastructure;
using Laraue.Apps.StructuredMessages.WebApiHost.Controllers;
using LinqToDB.EntityFrameworkCore;

namespace Laraue.Apps.StructuredMessages.IntegrationTests;

[Collection("IntegrationTest")]
public class MassMoveControllerTests(WebApiTestHost host) : IClassFixture<WebApiTestHost>
{
    private readonly Proxy<MassMovementController> _controller = host.Controller<MassMovementController>();
    
    [Fact]
    public async Task User_ShouldMoveNotDefaultSpace_WhenHasMassMovePermission()
    {
        using var testScope = host.CreateTestScope();
        var userId = await testScope.CreateUser();
        var personalOrganization = await testScope.InitializePersonalOrganization(
            userId,
            o => o.AddSpace(userId));
        var organization = await testScope.InitializeOrganization(userId);

        var spaceToMove = personalOrganization.GetSpace(1);
        
        await _controller
            .WithOrganizationAuthorization(organization.Id, userId)
            .Execute(x => x.MoveSpace(spaceToMove.Id, organization.Id));

        var resultSpace = await testScope.Database.Spaces.FirstAsyncEF(e => e.Id == spaceToMove.Id);
        Assert.Equal(organization.Id, resultSpace.OrganizationId);
    }
}