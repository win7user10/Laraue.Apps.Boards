using System.ComponentModel.DataAnnotations;
using Laraue.Apps.StructuredMessages.DataAccess;
using Laraue.Apps.StructuredMessages.DataAccess.Enums;
using Laraue.Apps.StructuredMessages.Services;
using Laraue.Core.DataAccess.EFCore.Extensions;
using Laraue.Core.Exceptions.Web;
using LinqToDB.EntityFrameworkCore;

namespace Laraue.Apps.StructuredMessages.WebApiServices;

public interface IEpicsService
{
    Task<EpicCountDto[]> GetEpicsWithCount(
        GetEpicsRequest request,
        CancellationToken cancellationToken);
    
    Task<CategoryDto> GetEpic(
        GetCategoryRequest request,
        CancellationToken cancellationToken);
    
    Task ChangeStatusesOrder(
        ChangeStatusesOrderRequest request,
        CancellationToken cancellationToken);
    
    Task<long> Create(
        CreateEpicRequest request,
        CancellationToken cancellationToken);
    
    Task Update(
        UpdateEpicRequest request,
        CancellationToken cancellationToken);
    
    Task Delete(
        DeleteCategoryRequest request,
        CancellationToken cancellationToken);
}

public class EpicsService(
    DatabaseContext context,
    ICoreEpicsService coreEpicsService,
    ICoreSpacesService coreSpacesService,
    IEpicsAccessService epicsAccessService,
    ISpacesAccessService spacesAccessService)
    : IEpicsService
{
    public async Task<EpicCountDto[]> GetEpicsWithCount(
        GetEpicsRequest request,
        CancellationToken cancellationToken)
    {
        return await context
            .Epics
            .Where(x => x.SpaceId == request.SpaceId)
            .Where(x => x.UserId == request.UserId)
            .OrderByDescending(x => x.TouchedAt)
            .Select(x => new EpicCountDto
            {
                Id = x.Id,
                Name = x.Name,
                IssuesCount = x.Statuses!.SelectMany(s => s.Issues!).Count(),
                Color = x.Color,
                StatusesCount = x.Statuses!.Count,
                TouchedAt = x.TouchedAt,
                IsDefault = x.IsDefault,
            })
            .ToArrayAsyncEF(cancellationToken);
    }

    public Task<CategoryDto> GetEpic(
        GetCategoryRequest request,
        CancellationToken cancellationToken)
    {
        return context
            .Epics
            .Where(x => x.Id == request.CategoryId)
            .Select(x => new CategoryDto
            {
                Color = x.Color,
                Name = x.Name,
                Statuses = x.Statuses!
                    .Select(s => new StatusDto
                    {
                        Id = s.Id,
                        Color = s.Color,
                        Name = s.Name,
                        SortOrder = s.SortOrder,
                    })
                    .ToArray(),
            })
            .FirstOrThrowNotFoundEFAsync($"Epic: {request.CategoryId} is not found", cancellationToken);
    }

    public async Task<long> Create(
        CreateEpicRequest request,
        CancellationToken cancellationToken)
    {
        await spacesAccessService
            .HasAccessOrThrow(
                request.AuthData,
                request.SpaceId,
                ItemAccessLevel.CreateItems,
                cancellationToken);
        
        return await coreEpicsService.Create(
            request.SpaceId,
            request.AuthData.UserId,
            request.Name,
            request.Color,
            statuses: null,
            cancellationToken);
    }

    public async Task ChangeStatusesOrder(
        ChangeStatusesOrderRequest request,
        CancellationToken cancellationToken)
    {
        await epicsAccessService
            .HasAccessOrThrow(
                request.AuthData,
                request.EpicId,
                ItemAccessLevel.UpdateSelf,
                cancellationToken);
        
        await coreEpicsService.ChangeStatusesOrder(
            new Services.ChangeStatusesOrderRequest
            {
                CategoryId = request.EpicId,
                Order = request.Order
            },
            cancellationToken);
    }

    public async Task Update(UpdateEpicRequest request, CancellationToken cancellationToken)
    {
        await epicsAccessService
            .HasAccessOrThrow(
                request.AuthData,
                request.Id,
                ItemAccessLevel.UpdateSelf,
                cancellationToken);

        await coreEpicsService.Update(
            request.Id,
            upd => upd
                .SetProperty(x => x.Color, request.Color)
                .SetProperty(x => x.Name, request.Name),
            cancellationToken);
    }

    public async Task Delete(DeleteCategoryRequest request, CancellationToken cancellationToken)
    {
        await epicsAccessService
            .HasAccessOrThrow(
                request.AuthData,
                request.Id,
                ItemAccessLevel.DeleteSelf,
                cancellationToken);
        
        await coreEpicsService.Delete(
            new DeleteRequest { Id = request.Id },
            cancellationToken);
    }
}

public record EpicCountDto
{
    public required long Id { get; set; }
    public required string Name { get; set; }
    public required int IssuesCount { get; set; }
    public required string? Color { get; set; }
    public required int StatusesCount { get; set; }
    public required DateTime TouchedAt { get; set; }
    public required bool IsDefault { get; set; }
}

public record GetCategoryRequest
{
    public required Guid UserId { get; set; }
    public required long CategoryId { get; set; }
}

public record CategoryDto
{
    public required string Name { get; set; }
    public required string? Color { get; set; }
    public StatusDto[] Statuses { get; set; } = [];
}

public class StatusDto
{
    public required long Id { get; set; }
    public required string Name { get; set; }
    public required string? Color { get; set; }
    public required int SortOrder { get; set; }
}

public record CreateEpicRequest
{
    public OrganizationAuthData AuthData { get; set; } = new();
    
    public long SpaceId { get; set; }
    
    [MaxLength(128)]
    public required string Name { get; set; }
    
    [MaxLength(7)]
    public required string Color { get; set; }
}

public record ChangeStatusesOrderRequest
{
    public OrganizationAuthData AuthData { get; set; } = new();
    public required long EpicId { get; set; }
    public required IReadOnlyDictionary<long, int> Order { get; set; }
}

public record UpdateEpicRequest
{
    public OrganizationAuthData AuthData { get; set; } = new();
    
    public long Id { get; set; }
    
    [MaxLength(128)]
    public required string Name { get; set; }
    
    [MaxLength(7)]
    public required string Color { get; set; }
}

public record DeleteCategoryRequest
{
    public OrganizationAuthData AuthData { get; set; } = new();
    
    public long Id { get; set; }
}

public record GetEpicsRequest
{
    public Guid UserId { get; set; }
    public long SpaceId { get; set; }
}