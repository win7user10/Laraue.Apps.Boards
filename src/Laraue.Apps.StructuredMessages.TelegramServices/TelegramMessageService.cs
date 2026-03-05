using Laraue.Apps.StructuredMessages.Services;
using Laraue.Telegram.NET.Core.Extensions;
using Laraue.Telegram.NET.Core.Utils;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace Laraue.Apps.StructuredMessages.TelegramServices;

public interface ITelegramMessageService
{
    Task SaveMessage(
        SaveTelegramMessageRequest request,
        CancellationToken cancellationToken);
}

public class TelegramMessageService(
    IMessageService messageService,
    ITelegramBotClient client)
    : ITelegramMessageService
{
    public async Task SaveMessage(
        SaveTelegramMessageRequest request,
        CancellationToken cancellationToken)
    {
        var id = await messageService.SaveMessage(
            new SaveMessageRequest
            {
                UserId = request.UserId,
                Text = request.Text,
                CreatedAt = request.SentAt,
            },
            cancellationToken);

        var types = await messageService.GetMessageTypes(
            request.UserId,
            cancellationToken);

        var tmb = new TelegramMessageBuilder()
            .Append($"The message saved with id: {id}");

        if (types.Length > 0)
        {
            tmb
                .AppendRow()
                .AppendRow("Select the category for message.");

            foreach (var typesChunked in types.Chunk(2))
            {
                var buttons = typesChunked
                    .Select(x => InlineKeyboardButton.WithCallbackData(x.Name));
            
                tmb.AddInlineKeyboardButtons(buttons);
            }
        }
        
        await client.SendTextMessageAsync(
            request.TelegramUserId,
            tmb,
            replyParameters: new ReplyParameters
            {
                MessageId = request.MessageId,
            },
            cancellationToken: cancellationToken);
    }
}

public class SaveTelegramMessageRequest
{
    public required long TelegramUserId { get; set; }
    public required int MessageId { get; set; }
    public required Guid UserId { get; set; }
    public required string Text { get; set; }
    public required DateTime SentAt { get; set; }
}