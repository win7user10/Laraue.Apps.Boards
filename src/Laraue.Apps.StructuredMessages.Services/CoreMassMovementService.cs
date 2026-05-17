using Laraue.Apps.StructuredMessages.DataAccess;
using Laraue.Core.DataAccess.EFCore.Extensions;
using Laraue.Core.Exceptions.Web;
using Microsoft.EntityFrameworkCore;

namespace Laraue.Apps.StructuredMessages.Services;

public interface ICoreMassMovementService
{
    /// <summary>
    /// Move space to the new organization.
    /// </summary>
    Task MoveSpace(
        long spaceId,
        long newOrganizationId,
        CancellationToken cancellationToken);
    
    /// <summary>
    /// Move space epics to the new space of any organization.
    /// </summary>
    Task MoveSpaceEpics(
        long spaceId,
        long newSpaceId,
        CancellationToken cancellationToken);
}

public class CoreMassMovementService(DatabaseContext context) : ICoreMassMovementService
{
    public async Task MoveSpace(long spaceId, long newOrganizationId, CancellationToken cancellationToken)
    {
        var sourceData = await context.Spaces
            .Where(x => x.Id == spaceId)
            .Select(x => new { x.IsDefault })
            .FirstOrThrowNotFoundEFAsync($"Space: {spaceId} is not found", cancellationToken);
        
        if (sourceData.IsDefault)
            throw new ForbiddenException("Default space cannot be moved. Move space epics instead.");

        await context.Spaces
            .Where(x => x.Id == spaceId)
            .ExecuteUpdateAsync(u => u
                    .SetProperty(p => p.OrganizationId, newOrganizationId),
                cancellationToken);
    }

    public async Task MoveSpaceEpics(long spaceId, long newSpaceId, CancellationToken cancellationToken)
    {
        await context.Epics
            .Where(x => x.SpaceId == spaceId)
            .Where(x => x.IsDefault == false)
            .ExecuteUpdateAsync(u => u
                    .SetProperty(epic => epic.SpaceId, newSpaceId),
                cancellationToken);
    }
}