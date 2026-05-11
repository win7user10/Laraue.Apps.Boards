using System.ComponentModel.DataAnnotations;
using Laraue.Apps.StructuredMessages.DataAccess;
using Laraue.Apps.StructuredMessages.DataAccess.Enums;
using Laraue.Apps.StructuredMessages.Services;
using Laraue.Core.DataAccess.Linq2DB.Extensions;
using Laraue.Core.Exceptions.Web;
using LinqToDB.EntityFrameworkCore;

namespace Laraue.Apps.StructuredMessages.WebApiServices;

public interface IEpicsService
{
    Task<EpicCountDto[]> GetSpaceEpics(
        GetEpicsRequest request,
        CancellationToken cancellationToken);
    
    Task<EpicDto> GetEpic(
        GetEpicRequest request,
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
        DeleteEpicRequest request,
        CancellationToken cancellationToken);
}

public class EpicsService(
    DatabaseContext context,
    ICoreEpicsService coreEpicsService,
    IEpicsAccessService epicsAccessService,
    ISpacesAccessService spacesAccessService)
    : IEpicsService
{
    public Task<EpicCountDto[]> GetSpaceEpics(
        GetEpicsRequest request,
        CancellationToken cancellationToken)
    {
        return epicsAccessService.GetAvailable(
            request.AuthData,
            new Filter { SpaceId = request.SpaceId },
            epics => epics
                .Select(x => new EpicCountDto
                {
                    Id = x.Epic.Id,
                    Name = x.Epic.Name,
                    IssuesCount = x.Epic.Statuses!.SelectMany(s => s.Issues!).Count(),
                    Color = x.Epic.Color,
                    StatusesCount = x.Epic.Statuses!.Count,
                    TouchedAt = x.Epic.TouchedAt,
                    IsDefault = x.Epic.IsDefault,
                })
                .ToArrayAsyncLinqToDB(cancellationToken),
            cancellationToken);
    }

    public async Task<EpicDto> GetEpic(
        GetEpicRequest request,
        CancellationToken cancellationToken)
    {
        var epic = await epicsAccessService.GetAvailable(
            request.AuthData,
            new Filter { EpicId = request.Id },
            epics => epics
                .Select(x => new EpicDto
                {
                    Color = x.Epic.Color,
                    Name = x.Epic.Name,
                    Statuses = x.Epic.Statuses!
                        .Select(s => new StatusDto
                        {
                            Id = s.Id,
                            Color = s.Color,
                            Name = s.Name,
                            SortOrder = s.SortOrder,
                        })
                        .ToArray(),
                    CanDelete = (x.EntityAccessLevel & EntityAccessLevel.Delete) == EntityAccessLevel.Delete,
                    CanUpdate = (x.EntityAccessLevel & EntityAccessLevel.Update) == EntityAccessLevel.Update,
                    CanViewIssues = false // Fill later
                })
                .FirstOrThrowNotFoundLinq2DbAsync($"Epic: {request.Id} is not found", cancellationToken),
            cancellationToken);
        
        epic.CanViewIssues = await epicsAccessService.HasAccess(
            request.AuthData,
            request.Id,
            ChildrenAccessLevel.Read,
            cancellationToken);

        return epic;
    }

    public async Task<long> Create(
        CreateEpicRequest request,
        CancellationToken cancellationToken)
    {
        if (!await spacesAccessService
            .CanCreateEpics(
                request.AuthData,
                request.SpaceId,
                cancellationToken))
            throw new NotFoundException(
                $"Space: {request.SpaceId} is not exists or items permission: {ChildrenAccessLevel.Create} is missing");
        
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
                ChildrenAccessLevel.Update,
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
                ChildrenAccessLevel.Update,
                cancellationToken);

        await coreEpicsService.Update(
            request.Id,
            upd => upd
                .SetProperty(x => x.Color, request.Color)
                .SetProperty(x => x.Name, request.Name),
            cancellationToken);
    }

    public async Task Delete(DeleteEpicRequest request, CancellationToken cancellationToken)
    {
        await epicsAccessService
            .HasAccessOrThrow(
                request.AuthData,
                request.Id,
                ChildrenAccessLevel.Delete,
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

public record GetEpicRequest
{
    public OrganizationAuthData AuthData { get; set; } = new();
    public required long Id { get; set; }
}

public record EpicDto
{
    public required string Name { get; set; }
    public required string? Color { get; set; }
    public StatusDto[] Statuses { get; set; } = [];
    public required bool CanViewIssues { get; set; }
    public required bool CanUpdate { get; set; }
    public required bool CanDelete { get; set; }
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

public record DeleteEpicRequest
{
    public OrganizationAuthData AuthData { get; set; } = new();
    
    public long Id { get; set; }
}

public record GetEpicsRequest
{
    public OrganizationAuthData AuthData { get; set; } = new();
    public long SpaceId { get; set; }
}