using Laraue.Apps.StructuredMessages.DataAccess;
using Laraue.Apps.StructuredMessages.DataAccess.Models;
using LinqToDB.EntityFrameworkCore;

namespace Laraue.Apps.StructuredMessages.Services;

public interface IMessageStatusService
{
    Task<long> Create(
        CreateMessageCategoryStatusRequest request,
        CancellationToken cancellationToken);

    Task<MessageStatusDto[]> GetStatuses(
        long categoryId,
        CancellationToken cancellationToken);
    
    Task<bool> UserHasAccessToStatus(
        Guid userId,
        long id,
        CancellationToken cancellationToken);
}

public class MessageStatusService(DatabaseContext context) : IMessageStatusService
{
    public async Task<long> Create(
        CreateMessageCategoryStatusRequest request,
        CancellationToken cancellationToken)
    {
        var previousMaxOrder = await context.MessageStatuses
            .Where(x => x.MessageCategoryId == request.CategoryId)
            .MaxAsyncEF(x => x.SortOrder, cancellationToken);
        
        var status = new MessageStatus
        {
            Name = request.Name,
            MessageCategoryId = request.CategoryId,
            SortOrder = ++previousMaxOrder
        };
        
        context.MessageStatuses.Add(status);
        await context.SaveChangesAsync(cancellationToken);

        return status.Id;
    }

    public Task<MessageStatusDto[]> GetStatuses(
        long categoryId,
        CancellationToken cancellationToken)
    {
        return context.MessageStatuses
            .Where(x => x.MessageCategoryId == categoryId)
            .Select(x => new MessageStatusDto
            {
                Id = x.Id,
                Name = x.Name,
            })
            .ToArrayAsyncEF(cancellationToken);
    }

    public Task<bool> UserHasAccessToStatus(Guid userId, long id, CancellationToken cancellationToken)
    {
        return context.MessageStatuses
            .Where(x => x.MessageCategory!.UserId == userId)
            .Where(x => x.Id == id)
            .AnyAsyncEF(cancellationToken);
    }
}

public class CreateMessageCategoryStatusRequest
{
    public required string Name { get; set; }
    public required long CategoryId { get; set; }
}

public class MessageStatusDto
{
    public required long Id { get; set; }
    public required string Name { get; set; }
}