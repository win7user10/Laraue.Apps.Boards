using Laraue.Apps.StructuredMessages.Services;
using Laraue.Telegram.NET.Core.Extensions;
using Laraue.Telegram.NET.Core.Routing;
using Laraue.Telegram.NET.Core.Routing.Attributes;
using Telegram.Bot;

namespace Laraue.Apps.StructuredMessages.TelegramHost.Controllers;

public class AllMessagesController(ITelegramBotClient client) : TelegramController
{
    [TelegramMessageRoute("(.*)")]
    public async Task Handle(
        RequestContext request,
        CancellationToken cancellationToken)
    {
        await client.SendMessage(
            request.Update.GetUserId(),
            $"Saved: {request.Update.Message!.Text}",
            cancellationToken: cancellationToken);
    }
}