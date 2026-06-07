using Laraue.Apps.Boards.TelegramServices.Services.Messages;
using Laraue.Telegram.NET.Core.Routing;

namespace Laraue.Apps.Boards.TelegramHost.Controllers;

public class MessagesController(ITelegramMessageService telegramMessageService)
    : TelegramController
{
}