using System.ComponentModel.DataAnnotations;
using Laraue.Apps.StructuredMessages.Services;
using Laraue.Core.Exceptions.Web;

namespace Laraue.Apps.StructuredMessages.WebApiServices;

public interface IStatusesService
{
    Task<long> CreateStatus(
        CreateStatusRequest request,
        CancellationToken cancellationToken);
}

public class StatusesService(
    ICoreCategoryService categoriesService,
    IMessageStatusService statusService) : IStatusesService
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
            },
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