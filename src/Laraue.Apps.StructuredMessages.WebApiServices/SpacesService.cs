using System.ComponentModel.DataAnnotations;
using Laraue.Apps.StructuredMessages.DataAccess;
using Laraue.Apps.StructuredMessages.DataAccess.Enums;
using Laraue.Apps.StructuredMessages.Services;
using LinqToDB.EntityFrameworkCore;

namespace Laraue.Apps.StructuredMessages.WebApiServices;

public interface ISpacesService
{
    Task<SpaceListDto[]> GetSpaces(
        GetSpacesRequest request,
        CancellationToken cancellationToken);
    
    Task<long> Create(
        CreateSpaceRequest request,
        CancellationToken cancellationToken);
    
    Task Update(
        UpdateSpaceRequest request,
        CancellationToken cancellationToken);
    
    Task Delete(
        DeleteSpaceRequest request,
        CancellationToken cancellationToken);
}

public class SpacesService(
    ICoreSpacesService coreSpacesService,
    ISpacesAccessService spacesAccessService,
    IOrganizationAccessService organizationAccessService)
    : ISpacesService
{
    public async Task<SpaceListDto[]> GetSpaces(
        GetSpacesRequest request,
        CancellationToken cancellationToken)
    {
        var spaces = await spacesAccessService.GetAvailableForRead(
            request.AuthData,
            items => items
                .Select(x => new SpaceListDto
                {
                    Id = x.Space.Id,
                    Name = x.Space.Name,
                    Color = x.Space.Color,
                    CanDelete = (x.EntityAccessLevel & EntityAccessLevel.Delete) == EntityAccessLevel.Delete,
                    CanUpdate = (x.EntityAccessLevel & EntityAccessLevel.Update) == EntityAccessLevel.Update,
                    CanCreateEpics = (x.ChildrenAccessLevel & ChildrenAccessLevel.Create) == ChildrenAccessLevel.Create,
                    Key = x.Space.Key,
                })
                .ToArrayAsyncLinqToDB(cancellationToken),
            cancellationToken);
        
        return spaces;
    }

    public async Task<long> Create(CreateSpaceRequest request, CancellationToken cancellationToken)
    {
        await organizationAccessService.CanCreateSpacesOrThrow(
            request.AuthData.OrganizationId,
            request.AuthData.UserId,
            cancellationToken);

        return await coreSpacesService.Create(
            request.AuthData.OrganizationId,
            request.AuthData.UserId,
            request.Key,
            request.Name,
            request.Color,
            cancellationToken);
    }

    public async Task Update(UpdateSpaceRequest request, CancellationToken cancellationToken)
    {
        await spacesAccessService.HasAccessOrThrow(
            request.AuthData,
            request.Id,
            EntityAccessLevel.Update,
            cancellationToken);

        await coreSpacesService.Update(
            request.Id,
            setters => setters
                .SetProperty(x => x.Color, request.Color)
                .SetProperty(x => x.Name, request.Name)
                .SetProperty(x => x.Key, request.Key.ToUpper()),
            cancellationToken);
    }

    public async Task Delete(DeleteSpaceRequest request, CancellationToken cancellationToken)
    {
        await spacesAccessService.HasAccessOrThrow(
            request.AuthData,
            request.Id,
            EntityAccessLevel.Delete,
            cancellationToken);
        
        await coreSpacesService.Delete(request.Id, cancellationToken);
    }
}

public record CreateSpaceRequest
{
    public OrganizationAuthData AuthData { get; set; } = new();
    
    [MaxLength(128)]
    [MinLength(3)]
    public required string Name { get; set; }
    
    [MaxLength(7)]
    [MinLength(7)]
    public required string Color { get; set; }
    
    [MaxLength(3)]
    [MinLength(3)]
    public required string Key { get; set; }
}

public record UpdateSpaceRequest
{
    public OrganizationAuthData AuthData { get; set; } = new();
    
    public long Id { get; set; }
    
    [MaxLength(128)]
    [MinLength(3)]
    public required string Name { get; set; }
    
    [MaxLength(7)]
    [MinLength(7)]
    public required string Color { get; set; }
    
    [MaxLength(3)]
    [MinLength(3)]
    public required string Key { get; set; }
}

public record DeleteSpaceRequest
{
    public OrganizationAuthData AuthData { get; set; } = new();
    public long Id { get; set; }
}

public record GetSpacesRequest
{
    public required OrganizationAuthData AuthData { get; set; }
}

public record SpaceListDto
{
    public long Id { get; set; }
    public required string Name { get; set; }
    public required string Color { get; set; }
    public required string Key { get; set; }
    public required bool CanUpdate { get; set; }
    public required bool CanDelete { get; set; }
    public required bool CanCreateEpics { get; set; }
}

public record SpaceDto
{
    public required bool CanCreateEpics { get; set; }
    public required bool CanUpdate { get; set; }
    public required bool CanDelete { get; set; }
}