using Laraue.Apps.StructuredMessages.DataAccess.Enums;
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
    
    [Fact]
    public async Task User_ShouldMoveNotDefaultEpic_WhenHasMassMovePermission()
    {
        using var testScope = host.CreateTestScope();
        var userId = await testScope.CreateUser();
        var personalOrganization = await testScope.InitializePersonalOrganization(
            userId,
            o => o
                .AddSpace(userId, s => s
                    .AddEpic(userId)));
        var organization = await testScope.InitializeOrganization(userId);
        var epicToMove = personalOrganization.GetEpic(1, 1);
        var spaceToReceive = organization.GetSpace(0);
        
        await _controller
            .WithOrganizationAuthorization(personalOrganization.Id, userId)
            .Execute(x => x.MoveEpic(epicToMove.Id, spaceToReceive.Id));
        
        var movedEpic = await testScope.Database.Epics.FirstAsyncEF(e => e.Id == epicToMove.Id);
        Assert.Equal(spaceToReceive.Id, movedEpic.SpaceId);
    }
    
    [Fact]
    public async Task User_ShouldNotMoveDefaultEpic_Always()
    {
        using var testScope = host.CreateTestScope();
        var userId = await testScope.CreateUser();
        var personalOrganization = await testScope.InitializePersonalOrganization(
            userId,
            o => o
                .AddSpace(userId, s => s
                    .AddEpic(userId)));
        var organization = await testScope.InitializeOrganization(userId);
        var epicToMove = personalOrganization.GetEpic(1, 0);
        var spaceToReceive = organization.GetSpace(0);
        
        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => _controller
            .WithOrganizationAuthorization(personalOrganization.Id, userId)
            .Execute(x => x.MoveEpic(epicToMove.Id, spaceToReceive.Id)));
        
        var forbidden = ex.HasInnerException<ForbiddenException>();
        Assert.Equal("Default epic cannot be moved.", forbidden.Message);
    }
    
    [Fact]
    public async Task User_ShouldMoveEpic_WhenHasMassMovePermission()
    {
        using var testScope = host.CreateTestScope();
        var userId = await testScope.CreateUser();
        var userCanNotMoveDueToMassMoveInSourceMissing = await testScope.CreateUser();
        var userCanNotMoveDueToEpicCreationInTargetMissing = await testScope.CreateUser();
        var userCanMove = await testScope.CreateUser();
        
        var sourceOrganization = await testScope.InitializeOrganization(
            userId,
            o => o
                .AddUser(userCanNotMoveDueToMassMoveInSourceMissing)
                .AddUser(userCanNotMoveDueToEpicCreationInTargetMissing, permissions => permissions
                    .SetAdminAccessLevel(AdminAccessLevel.MassMove))
                .AddUser(userCanMove, permissions => permissions
                    .SetAdminAccessLevel(AdminAccessLevel.MassMove))
                .AddSpace(userId, s => s
                    .AddEpic(userId)));
        
        var destinationOrganization = await testScope.InitializeOrganization(
            userId,
            o =>
                o
                    .AddUser(userCanNotMoveDueToEpicCreationInTargetMissing)
                    .AddUser(userCanMove, permissions => permissions
                    .SetEpicsAccessLevel(ChildrenAccessLevel.Create)));
        
        var epicToMove = sourceOrganization.GetEpic(1, 1);
        var spaceToReceive = destinationOrganization.GetSpace(0);
        
        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => _controller
            .WithOrganizationAuthorization(sourceOrganization.Id, userCanNotMoveDueToMassMoveInSourceMissing)
            .Execute(x => x.MoveEpic(epicToMove.Id, spaceToReceive.Id)));
        
        var notFound = ex.HasInnerException<NotFoundException>();
        Assert.Equal($"Organization: {sourceOrganization.Id} is unavailable or permission: MassMove is missing", notFound.Message);
        
        ex = await Assert.ThrowsAsync<HttpRequestException>(() => _controller
            .WithOrganizationAuthorization(sourceOrganization.Id, userCanNotMoveDueToEpicCreationInTargetMissing)
            .Execute(x => x.MoveEpic(epicToMove.Id, spaceToReceive.Id)));
        
        var forbidden = ex.HasInnerException<ForbiddenException>();
        Assert.Equal($"Space is not exists: {spaceToReceive.Id} or epic creation is forbidden", forbidden.Message);
        
        await _controller
            .WithOrganizationAuthorization(sourceOrganization.Id, userCanMove)
            .Execute(x => x.MoveEpic(epicToMove.Id, spaceToReceive.Id));
        
        var movedEpic = await testScope.Database.Epics.FirstAsyncEF(e => e.Id == epicToMove.Id);
        Assert.Equal(spaceToReceive.Id, movedEpic.SpaceId);
    }

    [Fact]
    public async Task GetDestinationSpaces_ShouldReturnOnlySpaces_WhereUserCanCreateEpics()
    {
        using var testScope = host.CreateTestScope();
        var userId = await testScope.CreateUser();
        var participatorId = await testScope.CreateUser();
        
        var sourceOrganization = await testScope.InitializeOrganization(
            userId,
            o => o
                .AddUser(participatorId, u => u
                    .SetAdminAccessLevel(AdminAccessLevel.MassMove)));
        
        var destinationOrganization = await testScope.InitializeOrganization(
            userId,
            o => o
                .AddSpace(userId)
                .AddSpace(userId, s => s.WithName("Allowed"))
                .AddUser(participatorId, u => u // User has create epics access only to last space
                    .SetSpaceEpicsAccessLevel(2, ChildrenAccessLevel.Create)));
        
        var allowedSpaces = await _controller
            .WithOrganizationAuthorization(sourceOrganization.Id, participatorId)
            .Execute(x => x.GetDestinationSpaces(destinationOrganization.Id));
        
        Assert.NotNull(allowedSpaces);
        var allowedSpace = Assert.Single(allowedSpaces);
        Assert.Equal("Allowed", allowedSpace.Name);
    }
}