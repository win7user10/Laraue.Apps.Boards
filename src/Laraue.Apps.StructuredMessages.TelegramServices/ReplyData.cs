using Laraue.Telegram.NET.Authentication.Services;
using Laraue.Telegram.NET.Core.Extensions;
using Telegram.Bot.Types;

namespace Laraue.Apps.StructuredMessages.TelegramServices;

public record ReplyData(Guid UserId, ChatId TelegramId, int MessageId)
    : TelegramMessageId(TelegramId, MessageId)
{
    public static ReplyData FromCallbackRequest(TelegramRequestContext<Guid> request)
    {
        return new ReplyData(
            request.UserId,
            request.Update.GetUserId(),
            request.Update.CallbackQuery.GetMessageId());
    }
    
    public static ReplyData FromMessageRequest(TelegramRequestContext<Guid> request)
    {
        return new ReplyData(
            request.UserId,
            request.Update.GetUserId(),
            request.Update.Message!.Id);
    }
}