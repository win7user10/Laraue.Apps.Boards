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
    
    Task MoveEpic(
        MoveEpicRequest request,
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
        await HasMassMovePermissionOrThrow(request.AuthData, cancellationToken);

        var sourceSpaceBelongsToCurrentOrganization = await context.Spaces
            .Where(x => x.Id == request.SpaceId)
            .Where(x => x.OrganizationId == request.AuthData.OrganizationId)
            .AnyAsyncEF(cancellationToken);
        
        if (!sourceSpaceBelongsToCurrentOrganization)
            throw new ForbiddenException($"Space is not exists: {request.SpaceId} in organization");

        await CanCreateEpicsOrThrow(request.AuthData, request.NewSpaceId, cancellationToken);

        await massMovementService.MoveSpaceEpics(request.SpaceId, request.NewSpaceId, cancellationToken);
    }

    public async Task MoveEpic(MoveEpicRequest request, CancellationToken cancellationToken)
    {
        await HasMassMovePermissionOrThrow(request.AuthData, cancellationToken);
        
        var sourceEpicBelongsToCurrentOrganization = await context.Epics
            .Where(x => x.Id == request.Id)
            .Where(x => x.Space!.OrganizationId == request.AuthData.OrganizationId)
            .AnyAsyncEF(cancellationToken);
        
        if (!sourceEpicBelongsToCurrentOrganization)
            throw new ForbiddenException($"Epic is not exists: {request.Id} in organization");
        
        await CanCreateEpicsOrThrow(request.AuthData, request.NewSpaceId, cancellationToken);
        
        await massMovementService.MoveEpic(request.Id, request.NewSpaceId, cancellationToken);
    }

    public async Task<DestinationSpace[]> GetDestinationSpaces(
        GetDestinationSpacesRequest request,
        CancellationToken cancellationToken)
    {
        await HasMassMovePermissionOrThrow(request.AuthData, cancellationToken);
        
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

    private async Task CanCreateEpicsOrThrow(
        OrganizationAuthData authData,
        long spaceId,
        CancellationToken cancellationToken)
    {
        var canCreateEpicsInNewSpace = await spacesAccessService.CanCreateEpics(
            authData,
            spaceId,
            cancellationToken);
        
        if (!canCreateEpicsInNewSpace)
            throw new ForbiddenException($"Space is not exists: {spaceId} or epic creation is forbidden");
    }
    
    private Task HasMassMovePermissionOrThrow(
        OrganizationAuthData authData,
        CancellationToken cancellationToken)
    {
        return organizationAccessService.HasAccessOrThrow(
            authData,
            AdminAccessLevel.MassMove,
            cancellationToken);
    }
}

public record MoveSpaceRequest
{
    public OrganizationAuthData AuthData { get; set; } = new();
    public long Id { get; set; }
    public long NewOrganizationId { get; set; }
}

public record MoveSpaceEpicsRequest
{
    public OrganizationAuthData AuthData { get; set; } = new();
    public long SpaceId { get; set; }
    public long NewSpaceId { get; set; }
}

public record MoveEpicRequest
{
    public OrganizationAuthData AuthData { get; set; } = new();
    public long Id { get; set; }
    public long NewSpaceId { get; set; }
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