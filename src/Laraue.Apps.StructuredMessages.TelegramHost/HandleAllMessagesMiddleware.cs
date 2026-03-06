using Laraue.Apps.StructuredMessages.TelegramServices;
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
            var text = context.Update.Message!.Text!;
            
            await telegramMessageService.HandleSaveMessage(
                new SaveMessageTelegramRequest
                {
                    Text = text,
                    MessageId = context.Update.Message.MessageId,
                    UserId = context.UserId,
                    TelegramUserId = context.Update.GetUserId(),
                    SentAt = context.Update.Message!.Date,
                },
                ct);
            
            context.SetExecutedRoute(new ExecutedRouteInfo("HandleAllMessagesMiddleware", text));
        }
    }
}