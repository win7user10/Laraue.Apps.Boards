using Laraue.Apps.StructuredMessages.IntegrationTests.Infrastructure;
using Laraue.Apps.StructuredMessages.WebApiHost.Controllers;
using Laraue.Core.Exceptions.Web;
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
    
    [Fact]
    public async Task User_ShouldNotMoveDefaultSpace_Always()
    {
        using var testScope = host.CreateTestScope();
        var userId = await testScope.CreateUser();
        var personalOrganization = await testScope.InitializePersonalOrganization(
            userId,
            o => o.AddSpace(userId));
        var organization = await testScope.InitializeOrganization(userId);

        var spaceToMove = personalOrganization.GetSpace(0);

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => _controller
            .WithOrganizationAuthorization(organization.Id, userId)
            .Execute(x => x.MoveSpace(spaceToMove.Id, organization.Id)));

        var forbidden = ex.HasInnerException<ForbiddenException>();
        Assert.Equal("Default space cannot be moved.", forbidden.Message);
    }
    
    [Fact]
    public async Task User_ShouldMoveSpaceEpics_WhenHasMassMovePermission()
    {
        using var testScope = host.CreateTestScope();
        var userId = await testScope.CreateUser();
        var personalOrganization = await testScope.InitializePersonalOrganization(
            userId,
            o => o
                .AddSpace(userId, s => s
                    .AddEpic(userId)));
        var organization = await testScope.InitializeOrganization(userId);

        var sourceSpace = personalOrganization.GetSpace(1);
        var backlogEpic = personalOrganization.GetEpic(1, 0); // Backlog should not be moved
        var epicToMove = personalOrganization.GetEpic(1, 1); // Other epics should be moved
        
        var spaceToReceive = organization.GetSpace(0);
        
        await _controller
            .WithOrganizationAuthorization(personalOrganization.Id, userId)
            .Execute(x => x.MoveSpaceEpics(sourceSpace.Id, spaceToReceive.Id));
        
        var movedEpic = await testScope.Database.Epics.FirstAsyncEF(e => e.Id == epicToMove.Id);
        Assert.Equal(spaceToReceive.Id, movedEpic.SpaceId);
        
        var notMovedEpic = await testScope.Database.Epics.FirstAsyncEF(e => e.Id == backlogEpic.Id);
        Assert.Equal(sourceSpace.Id, notMovedEpic.SpaceId);
    }
}