using Laraue.Apps.StructuredMessages.TelegramServices.Services.Messages;
using Laraue.Telegram.NET.Core.Routing;

namespace Laraue.Apps.StructuredMessages.TelegramHost.Controllers;

public class MessagesController(ITelegramMessageService telegramMessageService)
    : TelegramController
{
}