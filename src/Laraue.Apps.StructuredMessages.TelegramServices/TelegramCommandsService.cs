using Laraue.Apps.StructuredMessages.TelegramServices.Resources;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace Laraue.Apps.StructuredMessages.TelegramServices;

public interface ITelegramCommandsService
{
    Task HandleStart(
        ReplyData replyData,
        CancellationToken cancellationToken);
}

public class TelegramCommandsService(
    ITelegramBotClient client,
    IOptions<MiniAppOptions> options)
    : ITelegramCommandsService
{
    public Task HandleStart(ReplyData replyData, CancellationToken cancellationToken)
    {
        var markup = new InlineKeyboardMarkup()
            .AddButton(InlineKeyboardButton
                .WithWebApp(
                    $"📋 {Phrases.OpenMiniApp}", new WebAppInfo
                    {
                        Url = options.Value.Url
                    }));
        
        return client.SendMessage(
            replyData.TelegramId,
            Phrases.Start,
            replyMarkup: markup,
            cancellationToken: cancellationToken);
    }
}