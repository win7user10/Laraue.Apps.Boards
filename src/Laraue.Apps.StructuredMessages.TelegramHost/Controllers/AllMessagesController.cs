using Laraue.Apps.StructuredMessages.TelegramServices;
using Laraue.Telegram.NET.Core.Extensions;
using Laraue.Telegram.NET.Core.Routing;
using Laraue.Telegram.NET.Core.Routing.Attributes;
using SaveTelegramMessageRequest = Laraue.Apps.StructuredMessages.TelegramServices.SaveTelegramMessageRequest;

namespace Laraue.Apps.StructuredMessages.TelegramHost.Controllers;

public class AllMessagesController(ITelegramMessageService telegramMessageService)
    : TelegramController
{
    [TelegramMessageRoute(".*")]
    public Task HandleMessages(
        RequestContext request,
        CancellationToken cancellationToken)
    {
        return telegramMessageService.SaveMessage(
            new SaveTelegramMessageRequest
            {
                Text = request.Update.Message!.Text!,
                UserId = request.UserId,
                TelegramUserId = request.Update.GetUserId(),
                SentAt = request.Update.Message!.Date,
            },
            cancellationToken);
    }
}