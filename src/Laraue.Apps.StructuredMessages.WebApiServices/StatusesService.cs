using System.ComponentModel.DataAnnotations;
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
}

public class StatusesService(
    ICoreCategoryService categoriesService,
    ICoreStatusService statusService) : IStatusesService
{
    public async Task<long> CreateStatus(
        CreateStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (!await categoriesService
            .UserHasAccessToCategory(request.UserId, request.CategoryId, cancellationToken))
            throw new BadRequestException(
                nameof(request.CategoryId),
                "Invalid category");

        return await statusService.Create(
            new CreateMessageCategoryStatusRequest
            {
                CategoryId = request.CategoryId,
                Name = request.Name,
                Color = request.Color,
            },
            cancellationToken);
    }

    public async Task Delete(DeleteStatusRequest request, CancellationToken cancellationToken)
    {
        if (!await statusService.UserHasAccessToStatus(
            request.UserId, request.Id, cancellationToken))
            throw new NotFoundException("Status is not found");

        await statusService.Delete(
            new Services.DeleteStatusRequest
            {
                Id = request.Id,
            },
            cancellationToken);
    }

    public async Task Edit(EditStatusRequest request, CancellationToken cancellationToken)
    {
        if (!await statusService.UserHasAccessToStatus(
                request.UserId, request.Id, cancellationToken))
            throw new NotFoundException("Status is not found");

        await statusService.Update(
            request.Id,
            upd => upd
                .SetProperty(x => x.Color, request.Color)
                .SetProperty(x => x.Name, request.Name),
            cancellationToken);
    }
}

public record CreateStatusRequest
{
    public Guid UserId { get; set; }
    
    [MaxLength(128)]
    public required string Name { get; set; }
    
    [MaxLength(7)]
    public required string Color { get; set; }
    
    public required long CategoryId { get; set; }
}

public record DeleteStatusRequest
{
    public Guid UserId { get; set; }
    public required long Id { get; set; }
}

public record EditStatusRequest
{
    public Guid UserId { get; set; }
    public long Id { get; set; }
    
    [MaxLength(7)]
    public required string Color { get; set; }
    public required string Name { get; set; }
}