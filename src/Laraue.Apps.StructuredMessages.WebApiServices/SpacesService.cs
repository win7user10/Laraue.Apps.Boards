using System.ComponentModel.DataAnnotations;
using Laraue.Apps.StructuredMessages.DataAccess.Enums;
using Laraue.Apps.StructuredMessages.Services;
using Laraue.Core.Exceptions.Web;
using LinqToDB.EntityFrameworkCore;

namespace Laraue.Apps.StructuredMessages.WebApiServices;

public interface ISpacesService
{
    Task<SpaceDto[]> GetSpaces(
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
    public async Task<SpaceDto[]> GetSpaces(
        GetSpacesRequest request,
        CancellationToken cancellationToken)
    {
        var spacesCount = await spacesAccessService.GetAvailable(
            request.AuthData,
            items => items
                .Select(x => new SpaceDto
                {
                    Id = x.Space.Id,
                    Name = x.Space.Name,
                    Color = x.Space.Color,
                    EpicsCount = x.Space.Epics!.Count
                })
                .ToArrayAsyncLinqToDB(cancellationToken),
            cancellationToken);
        
        return spacesCount;
    }

    public async Task<long> Create(CreateSpaceRequest request, CancellationToken cancellationToken)
    {
        await organizationAccessService.HasAccessOrThrow(
            request.AuthData,
            AccessLevel.CreateItems,
            cancellationToken);

        return await coreSpacesService.Create(
            request.AuthData.OrganizationId,
            request.AuthData.UserId,
            request.Name,
            request.Color,
            cancellationToken);
    }

    public async Task Update(UpdateSpaceRequest request, CancellationToken cancellationToken)
    {
        await organizationAccessService.HasAccessOrThrow(
            request.AuthData,
            AccessLevel.UpdateItems,
            cancellationToken); // Wrong. Update should be available on space level (when managed is active??). It's strange

        await coreSpacesService.Update(
            request.Id,
            setters => setters
                .SetProperty(x => x.Color, request.Color)
                .SetProperty(x => x.Name, request.Name),
            cancellationToken);
    }

    public async Task Delete(DeleteSpaceRequest request, CancellationToken cancellationToken)
    {
        await spacesAccessService.HasAccessOrThrow(
            request.AuthData,
            request.Id,
            AccessLevel.DeleteItems,
            cancellationToken);
        
        await coreSpacesService.Delete(request.Id, cancellationToken);
    }
}

public record CreateSpaceRequest
{
    public OrganizationAuthData AuthData { get; set; } = new();
    
    [MaxLength(128)]
    public required string Name { get; set; }
    
    [MaxLength(7)]
    [MinLength(7)]
    public required string Color { get; set; }
}

public record UpdateSpaceRequest
{
    public OrganizationAuthData AuthData { get; set; } = new();
    
    public long Id { get; set; }
    
    [MaxLength(128)]
    public required string Name { get; set; }
    
    [MaxLength(7)]
    [MinLength(7)]
    public required string Color { get; set; }
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

public record SpaceDto
{
    public long Id { get; set; }
    public required string Name { get; set; }
    public required string? Color { get; set; }
    public required int EpicsCount { get; set; }
}