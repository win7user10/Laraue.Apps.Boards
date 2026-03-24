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
        var appUrl = options.Value.Url;
        
        var markup = new InlineKeyboardMarkup()
            .AddButton(InlineKeyboardButton
                .WithWebApp(
                    $"📋 {string.Format(Phrases.OpenMiniApp)}", new WebAppInfo
                    {
                        Url = appUrl
                    }));
        
        return client.SendMessage(
            replyData.TelegramId,
            string.Format(Phrases.Start, appUrl),
            replyMarkup: markup,
            cancellationToken: cancellationToken);
    }
}