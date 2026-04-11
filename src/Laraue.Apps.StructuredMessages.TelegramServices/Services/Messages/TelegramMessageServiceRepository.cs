using Laraue.Apps.StructuredMessages.DataAccess;
using LinqToDB.EntityFrameworkCore;

namespace Laraue.Apps.StructuredMessages.TelegramServices.Services.Messages;

public interface ITelegramMessageServiceRepository
{
    Task<MessageDto> GetMessage(
        long id,
        CancellationToken cancellationToken = default);
    
    Task<int> GetExternalTelegramMessageId(
        long telegramMessageId,
        CancellationToken cancellationToken = default);
}

public class TelegramMessageServiceRepository(DatabaseContext databaseContext)
    : ITelegramMessageServiceRepository
{
    public Task<MessageDto> GetMessage(
        long id,
        CancellationToken cancellationToken = default)
    {
        return databaseContext.Cards
            .Where(x => x.Id == id)
            .Select(x => new MessageDto
            {
                UserId = x.UserId,
                CategoryId = x.EpicId,
                CategoryName = x.Epic!.Name,
                Id = x.Id,
                StatusId = x.StatusId,
                StatusName = x.Status!.Name,
                UserTelegramId = x.User!.TelegramId,
                TelegramMessageId = x.TelegramMessageId,
                Content = x.Content,
            })
            .FirstAsyncEF(cancellationToken);
    }

    public Task<int> GetExternalTelegramMessageId(
        long telegramMessageId,
        CancellationToken cancellationToken = default)
    {
        return databaseContext.TelegramMessages
            .Where(x => x.Id == telegramMessageId)
            .Select(x => x.ExternalMessageId)
            .FirstOrDefaultAsyncEF(cancellationToken);
    }
}

public class MessageDto
{
    public required long Id { get; set; }
    public required Guid UserId { get; set; }
    public long? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public long? StatusId { get; set; }
    public string? StatusName { get; set; }
    public required long UserTelegramId { get; set; }
    public required long? TelegramMessageId { get; set; }
    public required string? Content { get; set; }
}