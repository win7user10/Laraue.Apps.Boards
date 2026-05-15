using System.ComponentModel.DataAnnotations;
using Laraue.Apps.StructuredMessages.DataAccess.Enums;
using Laraue.Apps.StructuredMessages.Services;
using Laraue.Core.Exceptions.Web;

namespace Laraue.Apps.StructuredMessages.WebApiServices;

public interface IStatusesService
{
    Task<long> CreateStatus(
        CreateStatusRequest request,
        CancellationToken cancellationToken);

    Task Delete(
        DeleteStatusRequest request,
        CancellationToken cancellationToken);
    
    Task Edit(
        EditStatusRequest request,
        CancellationToken cancellationToken);
    
    Task<MessageStatusDto[]> GetStatuses(
        GetStatusesRequest request,
        CancellationToken cancellationToken);
}

public class StatusesService(
    ICoreStatusService statusService,
    IEpicsAccessService epicsAccessService,
    IStatusAccessService statusAccessService)
    : IStatusesService
{
    public async Task<long> CreateStatus(
        CreateStatusRequest request,
        CancellationToken cancellationToken)
    {
        await epicsAccessService.HasAccessOrThrow(
            request.AuthData,
            request.EpicId,
            ChildrenAccessLevel.Update,
            cancellationToken);

        return await statusService.Create(
            new CreateMessageCategoryStatusRequest
            {
                CategoryId = request.EpicId,
                Name = request.Name,
                Color = request.Color,
            },
            cancellationToken);
    }

    public async Task Delete(DeleteStatusRequest request, CancellationToken cancellationToken)
    {
        await statusAccessService.CanModifyStatusOrThrow(
            request.AuthData,
            request.Id,
            cancellationToken);

        await statusService.Delete(
            new Services.DeleteStatusRequest
            {
                Id = request.Id,
            },
            cancellationToken);
    }

    public async Task Edit(EditStatusRequest request, CancellationToken cancellationToken)
    {
        await statusAccessService.CanModifyStatusOrThrow(
            request.AuthData,
            request.Id,
            cancellationToken);

        await statusService.Update(
            request.Id,
            upd => upd
                .SetProperty(x => x.Color, request.Color)
                .SetProperty(x => x.Name, request.Name),
            cancellationToken);
    }

    public async Task<MessageStatusDto[]> GetStatuses(GetStatusesRequest request, CancellationToken cancellationToken)
    {
        await epicsAccessService.HasAccessOrThrow(
            request.AuthData,
            request.EpicId,
            ChildrenAccessLevel.Read,
            cancellationToken);

        return await statusService.GetStatuses(
            request.EpicId,
            cancellationToken);
    }
}

public record CreateStatusRequest
{
    public OrganizationAuthData AuthData { get; set; } = new();
    
    [MaxLength(128)]
    public required string Name { get; set; }
    
    [MaxLength(7)]
    public required string Color { get; set; }
    
    public required long EpicId { get; set; }
}

public record DeleteStatusRequest
{
    public OrganizationAuthData AuthData { get; set; } = new();
    public required long Id { get; set; }
}

public record EditStatusRequest
{
    public OrganizationAuthData AuthData { get; set; } = new();
    public long Id { get; set; }
    
    [MaxLength(7)]
    public required string Color { get; set; }
    public required string Name { get; set; }
}

public record GetStatusesRequest
{
    public OrganizationAuthData AuthData { get; set; } = new();
    public required long EpicId { get; set; }
}