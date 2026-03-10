using System.ComponentModel.DataAnnotations;
using Laraue.Apps.StructuredMessages.DataAccess;
using Laraue.Apps.StructuredMessages.DataAccess.Models;
using Laraue.Core.DataAccess.EFCore.Extensions;

namespace Laraue.Apps.StructuredMessages.WebApiServices;

public interface IStatusesService
{
    Task<long> CreateStatus(
        CreateStatusRequest request,
        CancellationToken cancellationToken);
}

public class StatusesService(DatabaseContext context) : IStatusesService
{
    public async Task<long> CreateStatus(
        CreateStatusRequest request,
        CancellationToken cancellationToken)
    {
        await context.MessageCategories
            .Where(m => m.UserId == request.UserId)
            .AnyOrThrowNotFoundEFAsync(
                m => m.Id == request.CategoryId,
                cancellationToken);
        
        var status = new MessageCategoryStatus
        {
            Name = request.Name,
            MessageCategoryId = request.CategoryId,
            Color = request.Color,
        };
        
        context.MessageStatuses.Add(status);
        await context.SaveChangesAsync(cancellationToken);

        return status.Id;
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