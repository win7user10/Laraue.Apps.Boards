using System.Text;
using Laraue.Apps.StructuredMessages.TelegramServices;
using Laraue.Telegram.NET.Core.Routing;
using Laraue.Telegram.NET.Core.Routing.Attributes;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace Laraue.Apps.StructuredMessages.TelegramHost.Controllers;

public class CommandsController(ITelegramBotClient client) : TelegramController
{
    [TelegramMessageRoute("/start")]
    public Task HandleGetCategories(
        RequestContext requestContext,
        CancellationToken cancellationToken,
        IOptions<MiniAppOptions> options)
    {
        var replyData = ReplyData.FromMessageRequest(requestContext);

        var tmb = new StringBuilder()
            .AppendLine("👋 Hey! I'm Message Board.")
            .AppendLine()
            .AppendLine("I help you turn important Telegram messages into organized Kanban boards — with statuses, filters, and custom attributes.")
            .AppendLine()
            .AppendLine("All messages you send to me will be appeared on your board.")
            .AppendLine()
            .Append("Tap the button below to open your board 👇");

        var markup = new InlineKeyboardMarkup()
            .AddButton(InlineKeyboardButton
            .WithWebApp(
                "📋 Open MessageBoard", new WebAppInfo
                {
                    Url = options.Value.Url
                }));
        
        return client.SendMessage(
            replyData.TelegramId,
            tmb.ToString(),
            replyMarkup: markup,
            cancellationToken: cancellationToken);
    }
}