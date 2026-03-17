using Laraue.Apps.StructuredMessages.DataAccess;
using Laraue.Apps.StructuredMessages.DataAccess.Models;
using Laraue.Apps.StructuredMessages.Services;
using Laraue.Core.DateTime.Services.Abstractions;
using Laraue.Core.Exceptions.Web;
using LinqToDB.EntityFrameworkCore;
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
    
    Task<MessageListDto[]> Search(
        SearchRequest request,
        CancellationToken ct);
}

public class MessagesService(
    DatabaseContext context,
    ICoreMessageService messageService,
    ICoreCategoryService categoryService,
    IDateTimeProvider dateTimeProvider)
    : IMessagesService
{
    public async Task<MessageListDto[]> GetMessages(
        GetMessagesRequest request,
        CancellationToken cancellationToken)
    {
        var categoryId = request.CategoryId == CoreMessageService.NullId
            ? null
            : request.CategoryId;
        
        var result = await ProjectToTemporaryDto(context
            .Messages
            .Where(x => x.UserId == request.UserId)
            .Where(x => x.CategoryId == categoryId)
            .OrderByDescending(x => x.Id))
            .ToArrayAsync(cancellationToken);

        return Project(result);
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

    public async Task<MessageListDto[]> Search(SearchRequest request, CancellationToken ct)
    {
        var query = context.Messages
            .Where(x => x.UserId == request.UserId);
        
        if (request.CategoryId.HasValue)
        {
            var categoryId = request.CategoryId == CoreMessageService.NullId
                ? null
                : request.CategoryId;

            query = query.Where(x => x.CategoryId == categoryId);
        }
        
        if (!string.IsNullOrEmpty(request.SearchString))
        {
            query = query
                .Where(x => EF.Functions.TrigramsAreNotWordSimilar(
                    x.Content,
                    request.SearchString));
        }

        var result = await ProjectToTemporaryDto(query).ToArrayAsyncEF(ct);
        return Project(result);
    }

    private static IQueryable<MessageListDtoData> ProjectToTemporaryDto(
        IQueryable<Message> queryable)
    {
        return queryable.Select(x => new MessageListDtoData
        {
            Id = x.Id,
            Content = x.Content,
            Time = x.CreatedAt,
            CategoryId = x.CategoryId ?? CoreMessageService.NullId,
            StatusId = x.StatusId ?? CoreMessageService.NullId,
            TelegramFirstName = x.User!.TelegramFirstName,
            TelegramLastName = x.User!.TelegramLastName,
            TelegramId = x.User.TelegramId,
            TelegramUsername = x.User.TelegramUserName,
        });
    }

    private static MessageListDto[] Project(
        IEnumerable<MessageListDtoData> source)
    {
        var list = new List<MessageListDto>();

        foreach (var item in source)
        {
            var sender = item.TelegramUsername;
            var initial = sender is not null ? sender[..2] : "";

            if (sender is null)
            {
                if (item.TelegramFirstName is not null && item.TelegramLastName is not null)
                {
                    sender = $"{item.TelegramFirstName} {item.TelegramLastName}";
                    initial = $"{item.TelegramFirstName[..1]}{item.TelegramLastName[..1]}";
                }
                else if (item.TelegramFirstName is not null)
                {
                    sender = item.TelegramFirstName;
                    initial = item.TelegramFirstName[..2];
                }
                else if (item.TelegramLastName is not null)
                {
                    sender = item.TelegramLastName;
                    initial = item.TelegramLastName[..2];
                }
                else
                {
                    sender = item.TelegramId.ToString();
                    initial = "ID";
                }
            }

            list.Add(new MessageListDto
            {
                Id = item.Id,
                StatusId = item.StatusId,
                Content = item.Content,
                CategoryId = item.CategoryId,
                Sender = sender,
                SenderInitial = initial,
                Time = item.Time
            });
        }

        return list.ToArray();
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

public class MessageListDtoData
{
    public required long Id { get; set; }
    public required DateTime Time { get; set; }
    public required long TelegramId { get; set; }
    public required string? TelegramUsername { get; set; }
    public required string? TelegramFirstName { get; set; }
    public required string? TelegramLastName { get; set; }
    public required string Content { get; set; }
    public required long CategoryId { get; set; }
    public required long StatusId { get; set; }
}

public class MessageListDto
{
    public required long Id { get; set; }
    public required DateTime Time { get; set; }
    public required string? Sender { get; set; }
    public string? SenderInitial { get; set; }
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
    public required string Content { get; set; }
}

public record EditMessageRequest
{
    public Guid UserId { get; set; }
    public long MessageId { get; set; }
    public required string Content { get; set; }
}

public record SearchRequest
{
    public Guid UserId { get; set; }
    public long? CategoryId { get; set; }
    public string? SearchString { get; set; }
}