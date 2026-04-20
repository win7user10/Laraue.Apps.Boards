using System.ComponentModel.DataAnnotations;
using Laraue.Apps.StructuredMessages.DataAccess;
using Laraue.Apps.StructuredMessages.DataAccess.Enums;
using Laraue.Apps.StructuredMessages.Services;
using Laraue.Core.Exceptions.Web;
using LinqToDB.EntityFrameworkCore;

namespace Laraue.Apps.StructuredMessages.WebApiServices;

public interface ISpacesService
{
    Task<GetSpacesResponse> GetSpaces(
        GetSpacesRequest request,
        CancellationToken cancellationToken);
    
    Task<long> Create(
        CreateSpaceRequest request,
        CancellationToken cancellationToken);
    
    Task Update(
        EditSpaceRequest request,
        CancellationToken cancellationToken);
    
    Task Delete(
        DeleteSpaceRequest request,
        CancellationToken cancellationToken);
}

public class SpacesService(ICoreSpacesService coreSpacesService, DatabaseContext context)
    : ISpacesService
{
    public async Task<GetSpacesResponse> GetSpaces(
        GetSpacesRequest request,
        CancellationToken cancellationToken)
    {
        var spaces = await context.Spaces
            .Where(x => x.CreatorId == request.UserId)
            .Select(x => new SpaceDto
            {
                Id = x.Id,
                Name = x.Name,
                Color = x.Color,
                EpicsCount = x.Epics!.Count
            })
            .ToArrayAsyncEF(cancellationToken);

        var noEpicsCount = await context.Epics
            .Where(x => x.UserId == request.UserId)
            .Where(x => x.SpaceId == null)
            .CountAsyncEF(cancellationToken);

        return new GetSpacesResponse
        {
            Spaces = spaces,
            NoSpaceEpicsCount = noEpicsCount
        };
    }

    public Task<long> Create(CreateSpaceRequest request, CancellationToken cancellationToken)
    {
        return coreSpacesService.Create(
            request.UserId,
            request.Name,
            request.Color,
            cancellationToken);
    }

    public async Task Update(EditSpaceRequest request, CancellationToken cancellationToken)
    {
        if (!await coreSpacesService.UserHasAccessToSpace(
            request.UserId,
            request.Id,
            AccessLevel.Update,
            cancellationToken))
            throw new NotFoundException();

        await coreSpacesService.Update(
            request.Id,
            setters => setters
                .SetProperty(x => x.Color, request.Color)
                .SetProperty(x => x.Name, request.Name),
            cancellationToken);
    }

    public async Task Delete(DeleteSpaceRequest request, CancellationToken cancellationToken)
    {
        if (!await coreSpacesService.UserHasAccessToSpace(
            request.UserId,
            request.Id,
            AccessLevel.Delete,
            cancellationToken))
            throw new NotFoundException();
        
        await coreSpacesService.Delete(request.Id, cancellationToken);
    }
}

public record CreateSpaceRequest
{
    public Guid UserId { get; set; }
    
    [MaxLength(128)]
    public required string Name { get; set; }
    
    [MaxLength(7)]
    [MinLength(7)]
    public required string Color { get; set; }
}

public record EditSpaceRequest
{
    public long Id { get; set; }
    public Guid UserId { get; set; }
    
    [MaxLength(128)]
    public required string Name { get; set; }
    
    [MaxLength(7)]
    [MinLength(7)]
    public required string Color { get; set; }
}

public record DeleteSpaceRequest
{
    public long Id { get; set; }
    public Guid UserId { get; set; }
}

public record GetSpacesRequest
{
    public Guid UserId { get; set; }
}

public record SpaceDto
{
    public long Id { get; set; }
    public required string Name { get; set; }
    public required string? Color { get; set; }
    public required int EpicsCount { get; set; }
}

public record GetSpacesResponse
{
    public required SpaceDto[] Spaces { get; set; }
    public required int NoSpaceEpicsCount { get; set; }
}