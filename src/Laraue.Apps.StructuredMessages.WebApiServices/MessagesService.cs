using Laraue.Apps.StructuredMessages.DataAccess;
using Laraue.Apps.StructuredMessages.Services;
using Laraue.Core.DateTime.Services.Abstractions;
using Laraue.Core.Exceptions.Web;
using Microsoft.EntityFrameworkCore;

namespace Laraue.Apps.StructuredMessages.WebApiServices;

public interface IMessagesService
{
    Task<MessageListDto[]> GetMessages(
        GetMessagesRequest request,
        CancellationToken cancellationToken);

    Task UpdateStatus(
        UpdateStatusRequest request,
        CancellationToken ct);
    
    Task UpdateCategory(
        UpdateCategoryRequest request,
        CancellationToken ct);
    
    Task DeleteMessage(
        DeleteMessageRequest request,
        CancellationToken ct);
    
    Task<long> CreateMessage(
        CreateMessageRequest request,
        CancellationToken ct);
    
    Task EditMessage(
        EditMessageRequest request,
        CancellationToken ct);
}

public class MessagesService(
    DatabaseContext context,
    ICoreMessageService messageService,
    ICoreCategoryService categoryService,
    IDateTimeProvider dateTimeProvider)
    : IMessagesService
{
    public Task<MessageListDto[]> GetMessages(
        GetMessagesRequest request,
        CancellationToken cancellationToken)
    {
        var categoryId = request.CategoryId == CoreMessageService.NullId
            ? null
            : request.CategoryId;
        
        return context
            .Messages
            .Where(x => x.UserId == request.UserId)
            .Where(x => x.CategoryId == categoryId)
            .OrderByDescending(x => x.Id)
            .Select(x => new MessageListDto
            {
                Id = x.Id,
                Sender = x.Sender,
                Content = x.Content,
                Time = x.CreatedAt,
                CategoryId = x.CategoryId ?? CoreMessageService.NullId,
                StatusId = x.StatusId ?? CoreMessageService.NullId,
            })
            .ToArrayAsync(cancellationToken);
    }

    public async Task UpdateStatus(UpdateStatusRequest request, CancellationToken ct)
    {
        if (!await messageService.UserHasAccessToMessage(request.UserId, request.MessageId, ct)) 
            throw new NotFoundException();
        
        await messageService.UpdateStatus(request.MessageId, request.StatusId, ct);
    }

    public async Task UpdateCategory(UpdateCategoryRequest request, CancellationToken ct)
    {
        if (!await messageService.UserHasAccessToMessage(request.UserId, request.MessageId, ct)) 
            throw new NotFoundException();
        
        await messageService.UpdateCategory(request.MessageId, request.CategoryId, ct);
    }

    public async Task DeleteMessage(DeleteMessageRequest request, CancellationToken ct)
    {
        if (!await messageService.UserHasAccessToMessage(request.UserId, request.MessageId, ct)) 
            throw new NotFoundException();

        await messageService.DeleteMessage(request.MessageId, ct);
    }

    public async Task<long> CreateMessage(CreateMessageRequest request, CancellationToken ct)
    {
        long? categoryId = request.CategoryId == CoreMessageService.NullId
            ? null
            : request.CategoryId;
        
        long? statusId = request.StatusId == CoreMessageService.NullId
            ? null
            : request.StatusId;

        if (categoryId.HasValue
            && !await categoryService.UserHasAccessToCategory(
                request.UserId, request.CategoryId, ct))
                    throw new BadRequestException(
                        nameof(categoryId),
                        "Category is not found");

        return await messageService.SaveMessage(
            new SaveMessageRequest
            {
                CreatedAt = dateTimeProvider.UtcNow,
                Sender = request.Sender,
                Text = request.Content,
                UserId = request.UserId,
                CategoryId = categoryId,
                StatusId = statusId,
            },
            ct);
    }

    public async Task EditMessage(EditMessageRequest request, CancellationToken ct)
    {
        if (!await messageService.UserHasAccessToMessage(request.UserId, request.MessageId, ct)) 
            throw new NotFoundException();
        
        await messageService.UpdateMessage(
            request.MessageId,
            upd => upd
                .SetProperty(x => x.Content, request.Content),
            ct);
    }
}

public record UpdateStatusRequest
{
    public Guid UserId { get; set; }
    public long MessageId { get; set; }
    public long StatusId { get; set; }
}

public record UpdateCategoryRequest
{
    public Guid UserId { get; set; }
    public long MessageId { get; set; }
    public long CategoryId { get; set; }
}

public record GetMessagesRequest
{
    public Guid UserId { get; set; }
    public long? CategoryId { get; set; }
}

public class MessageListDto
{
    public required long Id { get; set; }
    public required DateTime Time { get; set; }
    public required string? Sender { get; set; }
    public string? SenderInitial => Sender?[..2];
    public required string Content { get; set; }
    public required long CategoryId { get; set; }
    public required long StatusId { get; set; }
}

public record DeleteMessageRequest
{
    public Guid UserId { get; set; }
    public long MessageId { get; set; }
}

public record CreateMessageRequest
{
    public Guid UserId { get; set; }
    public long CategoryId { get; set; }
    public long StatusId { get; set; }
    public string? Sender { get; set; }
    public required string Content { get; set; }
}

public record EditMessageRequest
{
    public Guid UserId { get; set; }
    public long MessageId { get; set; }
    public required string Content { get; set; }
}