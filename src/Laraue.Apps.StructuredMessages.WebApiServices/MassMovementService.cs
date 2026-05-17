using Laraue.Apps.StructuredMessages.DataAccess;
using Laraue.Apps.StructuredMessages.DataAccess.Enums;
using Laraue.Apps.StructuredMessages.Services;
using Laraue.Core.Exceptions.Web;
using LinqToDB.EntityFrameworkCore;

namespace Laraue.Apps.StructuredMessages.WebApiServices;

public interface IMassMovementService
{
    Task MoveSpace(
        MoveSpaceRequest request,
        CancellationToken cancellationToken);
    
    Task MoveSpaceEpics(
        MoveSpaceEpicsRequest request,
        CancellationToken cancellationToken);
    
    Task<DestinationSpace[]> GetDestinationSpaces(
        GetDestinationSpacesRequest request,
        CancellationToken cancellationToken);
}

public class MassMovementService(
    ICoreMassMovementService massMovementService,
    ISpacesAccessService spacesAccessService,
    IOrganizationAccessService organizationAccessService,
    DatabaseContext context)
    : IMassMovementService
{
    public async Task MoveSpace(MoveSpaceRequest request, CancellationToken cancellationToken)
    {
        await organizationAccessService.HasAccessOrThrow(
            request.AuthData,
            AdminAccessLevel.MassMove,
            cancellationToken);

        await organizationAccessService.CanCreateSpacesOrThrow(
            request.NewOrganizationId,
            request.AuthData.UserId,
            cancellationToken);
            
        await massMovementService.MoveSpace(request.Id, request.NewOrganizationId, cancellationToken);
    }

    public async Task MoveSpaceEpics(MoveSpaceEpicsRequest request, CancellationToken cancellationToken)
    {
        await organizationAccessService.HasAccessOrThrow(
            request.AuthData,
            AdminAccessLevel.MassMove,
            cancellationToken);

        var sourceSpaceBelongsToCurrentOrganization = await context.Spaces
            .Where(x => x.Id == request.SpaceId)
            .Where(x => x.OrganizationId == request.AuthData.OrganizationId)
            .AnyAsyncEF(cancellationToken);
        
        if (!sourceSpaceBelongsToCurrentOrganization)
            throw new ForbiddenException($"Space is not exists: {request.SpaceId} in organization");

        var canCreateEpicsInNewSpace = await spacesAccessService.CanCreateEpics(
            request.AuthData,
            request.NewSpaceId,
            cancellationToken);
        
        if (!canCreateEpicsInNewSpace)
            throw new ForbiddenException($"Space is not exists: {request.NewSpaceId} or epic creation is forbidden");

        await massMovementService.MoveSpaceEpics(request.SpaceId, request.NewSpaceId, cancellationToken);
    }

    public async Task<DestinationSpace[]> GetDestinationSpaces(
        GetDestinationSpacesRequest request,
        CancellationToken cancellationToken)
    {
        await organizationAccessService.HasAccessOrThrow(
            request.AuthData,
            AdminAccessLevel.MassMove,
            cancellationToken);
        
        return await spacesAccessService.GetAvailableForRead(
            request.AuthData with { OrganizationId = request.OrganizationId },
            query => query
                .Where(q => (q.ChildrenAccessLevel & ChildrenAccessLevel.Create) == ChildrenAccessLevel.Create)
                .Select(x => new DestinationSpace
                {
                    Id = x.Space.Id,
                    Color = x.Space.Color,
                    Name = x.Space.Name,
                })
                .ToArrayAsyncLinqToDB(cancellationToken),
            cancellationToken);
    }
}

public record GetDestinationSpacesRequest
{
    public required OrganizationAuthData AuthData { get; set; }
    public required long OrganizationId { get; set; }
}

public record DestinationSpace
{
    public required long Id { get; set; }
    public required string Name { get; set; }
    public required string Color { get; set; }
}