using Laraue.Apps.StructuredMessages.TelegramServices;
using Laraue.Apps.StructuredMessages.TelegramServices.Services.Messages;
using Laraue.Telegram.NET.Abstractions;
using Laraue.Telegram.NET.Core.Extensions;
using Telegram.Bot.Types.Enums;

namespace Laraue.Apps.StructuredMessages.TelegramHost;

public class HandleAllMessagesMiddleware(
    RequestContext context,
    ITelegramMessageService telegramMessageService)
    : ITelegramMiddleware
{
    public async Task InvokeAsync(Func<CancellationToken, Task> next, CancellationToken ct)
    {
        await next(ct);
        
        if (context.GetExecutedRoute() is null && context.Update.Type == UpdateType.Message)
        {
            var message = context.Update.Message!;
            var text = message.Text!;
            
            await telegramMessageService.HandleSaveMessage(
                new SaveMessageTelegramRequest
                {
                    Text = text,
                    TelegramMessageId = message.MessageId,
                    UserId = context.UserId,
                    TelegramUserId = context.Update.GetUserId(),
                    SentAt = message.Date,
                    From = message.From?.Username,
                },
                ct);
            
            context.SetExecutedRoute(
                new ExecutedRouteInfo("HandleAllMessagesMiddleware", text));
        }
    }
}