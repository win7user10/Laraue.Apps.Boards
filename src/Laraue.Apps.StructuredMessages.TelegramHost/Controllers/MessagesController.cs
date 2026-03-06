using Laraue.Apps.StructuredMessages.TelegramServices;
using Laraue.Telegram.NET.Abstractions.Request;
using Laraue.Telegram.NET.Core.Routing;
using Laraue.Telegram.NET.Core.Routing.Attributes;

namespace Laraue.Apps.StructuredMessages.TelegramHost.Controllers;

public class MessagesController(ITelegramMessageService telegramMessageService)
    : TelegramController
{
    [TelegramCallbackRoute(TelegramRoutes.SetMessageCategory)]
    public Task SetMessageCategory(
        [FromQuery] UpdateMessageCategoryTelegramRequest request,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        return telegramMessageService.HandleUpdateMessageCategory(
            ReplyData.FromCallbackRequest(context),
            request,
            cancellationToken);
    }
}