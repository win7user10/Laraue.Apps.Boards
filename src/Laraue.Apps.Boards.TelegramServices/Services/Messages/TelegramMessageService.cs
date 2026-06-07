using Telegram.Bot;
using Telegram.Bot.Types;

namespace Laraue.Apps.Boards.TelegramServices.Services.Messages;

public class TelegramMessageService(
    ITelegramBotClient client,
    ITelegramSaveMessageService saveMessageService)
    : ITelegramMessageService
{
    public async Task HandleSaveMessage(
        SaveMessageTelegramRequest request,
        CancellationToken cancellationToken)
    {
        var result = await saveMessageService.Save(
            request,
            cancellationToken);

        // If message was created with that request then response,
        // otherwise it is the second, third etc. parts of message
        if (result.Result is Result.MainMessageCreated)
            await SetReaction(request, "👍", cancellationToken);
        else if (result.Result is Result.MainMessageUpdated)
            await SetReaction(request, "❤", cancellationToken);
    }

    private async Task SetReaction(
        SaveMessageTelegramRequest request,
        string? reaction,
        CancellationToken ct)
    {
        await client.SetMessageReaction(
            request.ExternalUserId,
            request.ExternalMessageId,
            reaction is not null
                ? [new ReactionTypeEmoji { Emoji = reaction }]
                : [],
            cancellationToken: ct);
    }
}