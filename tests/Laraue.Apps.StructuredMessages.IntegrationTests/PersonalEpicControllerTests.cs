using Laraue.Apps.StructuredMessages.IntegrationTests.Infrastructure;
using Laraue.Apps.StructuredMessages.WebApiHost.Controllers;
using Laraue.Apps.StructuredMessages.WebApiServices;
using LinqToDB.EntityFrameworkCore;

namespace Laraue.Apps.StructuredMessages.IntegrationTests;

public class PersonalEpicControllerTests(WebApiTestHost host) : IClassFixture<WebApiTestHost>
{
    private readonly Proxy<EpicsController> _epicsController = host.Controller<EpicsController>();
    
    [Fact]
    public async Task User_ShouldCreatePersonalEpic_Always()
    {
        using var testScope = host.CreateTestScope();
        var userId = await testScope.CreateUser();
        var organization = await testScope.InitializePersonalOrganization(
            userId,
            setup => setup.AddSpace(userId));

        var spaceId = organization.Spaces![1].Id;
        
        var epicId = await _epicsController
            .WithOrganizationAuthorization(organization.Id, userId)
            .Execute(x => x.Create(
                new CreateEpicRequest
                {
                    Name = "Epic 1",
                    Color = "#ffffff",
                    SpaceId = spaceId,
                }));

        var epic = await testScope.Database.Epics.FirstAsyncEF(e => e.Id == epicId);
        
        Assert.Equal("Epic 1", epic.Name);
        Assert.Equal("#ffffff", epic.Color);
        Assert.Equal(userId, epic.UserId);
        Assert.True(epic.CreatedAt != default);
        Assert.True(epic.UpdatedAt != default);
        Assert.True(epic.TouchedAt != default);
        Assert.False(epic.IsDefault);
    }
}