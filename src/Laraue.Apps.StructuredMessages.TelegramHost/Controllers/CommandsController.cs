using Laraue.Apps.StructuredMessages.TelegramServices;
using Laraue.Telegram.NET.Core.Routing;
using Laraue.Telegram.NET.Core.Routing.Attributes;

namespace Laraue.Apps.StructuredMessages.TelegramHost.Controllers;

public class CommandsController(ITelegramCommandsService commandsService)
    : TelegramController
{
    [TelegramMessageRoute("/start")]
    public Task HandleGetCategories(
        RequestContext requestContext,
        CancellationToken cancellationToken)
    {
        return commandsService.HandleStart(
            ReplyData.FromMessageRequest(requestContext),
            cancellationToken);
    }
}