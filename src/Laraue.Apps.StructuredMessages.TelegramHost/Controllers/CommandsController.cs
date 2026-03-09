using Laraue.Apps.StructuredMessages.TelegramServices;
using Laraue.Telegram.NET.Core.Extensions;
using Laraue.Telegram.NET.Core.Routing;
using Laraue.Telegram.NET.Core.Routing.Attributes;
using Laraue.Telegram.NET.Core.Utils;
using Telegram.Bot;

namespace Laraue.Apps.StructuredMessages.TelegramHost.Controllers;

public class CommandsController(ITelegramBotClient client) : TelegramController
{
    [TelegramMessageRoute("/start")]
    public Task HandleGetCategories(
        RequestContext requestContext,
        CancellationToken cancellationToken)
    {
        var replyData = ReplyData.FromMessageRequest(requestContext);
        
        return client.SendTextMessageAsync(
            replyData.TelegramId,
            new TelegramMessageBuilder()
                .AppendRow("Welcome to the Bot that helps organize messages in the Board")
                .AppendRow()
                .AppendRow("Any message you send or forward will be saved by the Bot.")
                .AppendRow("This message can be assigned to category and the status for it can be chosen")
                .AppendRow()
                .AppendRow("All messages then will be available in Mini App with user-friendly interface")
                .AppendRow()
                .AppendRow("The Bot in the active development phase. It can work not as excepted yet."),
            cancellationToken: cancellationToken);
    }
}