using Laraue.Apps.StructuredMessages.DataAccess;
using Laraue.Apps.StructuredMessages.DataAccess.Enums;
using Laraue.Apps.StructuredMessages.Services;
using Laraue.Core.DataAccess.EFCore.Extensions;
using Laraue.Core.Exceptions.Web;
using LinqToDB.EntityFrameworkCore;

namespace Laraue.Apps.StructuredMessages.WebApiServices;

public interface IMovementService
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

    Task MoveIssue(
        MoveIssueRequest request,
        CancellationToken ct);
}

public class MovementService(
    ICoreMovementService movementService,
    ISpacesAccessService spacesAccessService,
    IOrganizationAccessService organizationAccessService,
    DatabaseContext context,
    IStatusAccessService statusAccessService,
    IIssuesAccessService issuesAccessService)
    : IMovementService
{
    public async Task MoveSpace(MoveSpaceRequest request, CancellationToken cancellationToken)
    {
        await HasMassMovePermissionOrThrow(request.AuthData, cancellationToken);

        await organizationAccessService.CanCreateSpacesOrThrow(
            request.NewOrganizationId,
            request.AuthData.UserId,
            cancellationToken);
            
        await movementService.MoveSpace(request.Id, request.NewOrganizationId, cancellationToken);
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

        await CanCreateEpicsOrThrow(request.AuthData.UserId, request.NewSpaceId, cancellationToken);

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        await movementService.MoveSpaceEpics(request.SpaceId, request.NewSpaceId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
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
        
        await CanCreateEpicsOrThrow(request.AuthData.UserId, request.NewSpaceId, cancellationToken);
        
        await movementService.MoveEpic(request.Id, request.NewSpaceId, cancellationToken);
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

    public async Task MoveIssue(MoveIssueRequest request, CancellationToken ct)
    {
        // Check that can move Issue
        await issuesAccessService.HasAccessOrThrow(
            request.AuthData,
            request.IssueId,
            EntityAccessLevel.Update,
            ct);
        
        // Check that can move to specified status
        await statusAccessService.CanMoveToStatusOrThrow(
            request.AuthData,
            request.StatusId,
            ct);
        
        await movementService.MoveIssue(
            request.IssueId,
            request.StatusId,
            ct);
    }

    private async Task CanCreateEpicsOrThrow(
        Guid userId,
        long spaceId,
        CancellationToken cancellationToken)
    {
        var organizationId = await context.Spaces
            .Where(x => x.Id == spaceId)
            .Select(x => x.OrganizationId)
            .FirstOrThrowNotFoundEFAsync(SpaceIsNotExistsError(spaceId), cancellationToken);

        var canCreateEpicsInNewSpace = await spacesAccessService.CanCreateEpics(
            new OrganizationAuthData { OrganizationId = organizationId, UserId = userId },
            spaceId,
            cancellationToken);
        
        if (!canCreateEpicsInNewSpace)
            throw new ForbiddenException(SpaceIsNotExistsError(spaceId));
    }

    private static string SpaceIsNotExistsError(long spaceId)
    {
        return $"Space is not exists: {spaceId} or epic creation is forbidden";
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

public record MoveIssueRequest
{
    public OrganizationAuthData AuthData { get; set; } = new();
    public long IssueId { get; set; }
    public long StatusId { get; set; }
}