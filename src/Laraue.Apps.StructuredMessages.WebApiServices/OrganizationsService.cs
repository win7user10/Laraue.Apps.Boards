using System.ComponentModel.DataAnnotations;
using Laraue.Apps.StructuredMessages.DataAccess;
using Laraue.Apps.StructuredMessages.Services;
using Laraue.Core.Exceptions.Web;
using LinqToDB.EntityFrameworkCore;

namespace Laraue.Apps.StructuredMessages.WebApiServices;

public interface IOrganizationsService
{
    Task<GetOrganizationsResponse> GetSpaces(
        GetOrganizationsRequest request,
        CancellationToken cancellationToken);
    
    Task<long> Create(
        CreateOrganizationRequest request,
        CancellationToken cancellationToken);
    
    Task Update(
        EditOrganizationRequest request,
        CancellationToken cancellationToken);
    
    Task Delete(
        DeleteOrganizationRequest request,
        CancellationToken cancellationToken);
}

public class OrganizationsService(ICoreOrganizationsService coreOrganizationsService, DatabaseContext context)
    : IOrganizationsService
{
    public async Task<GetOrganizationsResponse> GetSpaces(
        GetOrganizationsRequest request,
        CancellationToken cancellationToken)
    {
        var organizations = await context.Organizations
            .Where(x => x.OwnerId == request.UserId)
            .Select(x => new OrganizationDto
            {
                Id = x.Id,
                Name = x.Name,
                Color = x.Color,
                SpacesCount = x.Spaces!.Count
            })
            .ToArrayAsyncEF(cancellationToken);

        var noSpacesCount = await context.Spaces
            .Where(x => x.CreatorId == request.UserId)
            .Where(x => x.OrganizationId == null)
            .CountAsyncEF(cancellationToken);

        return new GetOrganizationsResponse
        {
            Organizations = organizations,
            PersonalOrganizationSpacesCount = noSpacesCount
        };
    }

    public Task<long> Create(CreateOrganizationRequest request, CancellationToken cancellationToken)
    {
        return coreOrganizationsService.Create(
            request.UserId,
            request.Name,
            request.Color,
            cancellationToken);
    }

    public async Task Update(EditOrganizationRequest request, CancellationToken cancellationToken)
    {
        if (!await coreOrganizationsService.HasAccess(
            request.UserId,
            request.Id,
            AccessType.Update,
            cancellationToken))
            throw new NotFoundException();

        await coreOrganizationsService.Update(
            request.Id,
            setters => setters
                .SetProperty(x => x.Color, request.Color)
                .SetProperty(x => x.Name, request.Name),
            cancellationToken);
    }

    public async Task Delete(DeleteOrganizationRequest request, CancellationToken cancellationToken)
    {
        if (!await coreOrganizationsService.HasAccess(
            request.UserId,
            request.Id,
            AccessType.Delete,
            cancellationToken))
            throw new NotFoundException();
        
        await coreOrganizationsService.Delete(request.Id, cancellationToken);
    }
}

public record CreateOrganizationRequest
{
    public Guid UserId { get; set; }
    
    [MaxLength(128)]
    public required string Name { get; set; }
    
    [MaxLength(7)]
    [MinLength(7)]
    public required string Color { get; set; }
}

public record EditOrganizationRequest
{
    public long Id { get; set; }
    public Guid UserId { get; set; }
    
    [MaxLength(128)]
    public required string Name { get; set; }
    
    [MaxLength(7)]
    [MinLength(7)]
    public required string Color { get; set; }
}

public record DeleteOrganizationRequest
{
    public long Id { get; set; }
    public Guid UserId { get; set; }
}

public record GetOrganizationsRequest
{
    public Guid UserId { get; set; }
}

public record OrganizationDto
{
    public long Id { get; set; }
    public required string Name { get; set; }
    public required string? Color { get; set; }
    public required int SpacesCount { get; set; }
}

public record GetOrganizationsResponse
{
    public required OrganizationDto[] Organizations { get; set; }
    public required int PersonalOrganizationSpacesCount { get; set; }
}