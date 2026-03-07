using Laraue.Apps.StructuredMessages.DataAccess;
using Laraue.Apps.StructuredMessages.DataAccess.Models;
using LinqToDB.EntityFrameworkCore;

namespace Laraue.Apps.StructuredMessages.Services;

public interface IMessageCategoryService
{
    Task<MessageCategoryDto> GetMessageCategory(
        long id,
        CancellationToken cancellationToken);
    
    Task<MessageCategoryListDto[]> GetMessageCategories(
        Guid userId,
        CancellationToken cancellationToken);
    
    Task<long> CreateMessageCategory(
        CreateMessageCategoryRequest request,
        CancellationToken cancellationToken);
}

public class MessageCategoryService(DatabaseContext context)
    : IMessageCategoryService
{
    public Task<MessageCategoryDto> GetMessageCategory(
        long id,
        CancellationToken cancellationToken)
    {
        return context.MessageTypes
            .Where(x => x.Id == id)
            .Select(x => new MessageCategoryDto
            {
                Name = x.Name,
            })
            .FirstAsyncEF(cancellationToken);
    }

    public Task<MessageCategoryListDto[]> GetMessageCategories(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return context.MessageTypes
            .Where(x => x.UserId == userId)
            .Select(x => new MessageCategoryListDto
            {
                Name = x.Name,
                Id = x.Id
            })
            .ToArrayAsyncEF(cancellationToken);
    }

    public async Task<long> CreateMessageCategory(
        CreateMessageCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var category = new MessageCategory
        {
            Name = request.Name,
            UserId = request.UserId
        };
        
        context.MessageTypes.Add(category);
        await context.SaveChangesAsync(cancellationToken);

        return category.Id;
    }
}

public class MessageCategoryListDto
{
    public long Id { get; set; }
    public required string Name { get; set; }
}

public class MessageCategoryDto
{
    public required string Name { get; set; }
}

public class CreateMessageCategoryRequest
{
    public required string Name { get; set; }
    public required Guid UserId { get; set; }
}