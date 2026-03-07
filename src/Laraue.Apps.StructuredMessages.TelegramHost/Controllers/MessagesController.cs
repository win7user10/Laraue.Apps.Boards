using Laraue.Apps.StructuredMessages.TelegramServices;
using Laraue.Apps.StructuredMessages.TelegramServices.Services;
using Laraue.Apps.StructuredMessages.TelegramServices.Services.Messages;
using Laraue.Telegram.NET.Abstractions.Request;
using Laraue.Telegram.NET.Core.Extensions;
using Laraue.Telegram.NET.Core.Routing;
using Laraue.Telegram.NET.Core.Routing.Attributes;

namespace Laraue.Apps.StructuredMessages.TelegramHost.Controllers;

public class MessagesController(ITelegramMessageService telegramMessageService)
    : TelegramController
{
    [TelegramCallbackRoute(TelegramRoutes.CreateCategoryFromMessage, RouteMethod.Post)]
    public async Task CreateCategory(
        [FromQuery] CreateCategoryFromMessageTelegramRequest request,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        await telegramMessageService.HandleCreateCategory(
            new HandleCreateCategoryFromMessageRequest
            {
                UserId = context.UserId,
                From = context.Update.GetUser()?.Username,
                MessageId = request.MessageId,
                TelegramUserId = context.Update.GetUserId(),
            },
            cancellationToken);
    }
    
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
    
    [TelegramCallbackRoute(TelegramRoutes.CreateStatusFromMessage, RouteMethod.Post)]
    public async Task CreateStatus(
        [FromQuery] CreateStatusFromMessageTelegramRequest request,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        await telegramMessageService.HandleCreateStatus(
            new HandleCreateStatusFromMessageRequest
            {
                UserId = context.UserId,
                MessageId = request.MessageId,
                TelegramUserId = context.Update.GetUserId(),
                MessageCategoryId = request.MessageCategoryId,
            },
            cancellationToken);
    }
    
    [TelegramCallbackRoute(TelegramRoutes.SetMessageStatus)]
    public Task SetMessageStatus(
        [FromQuery] UpdateMessageStatusTelegramRequest request,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        return telegramMessageService.HandleUpdateStatus(
            ReplyData.FromCallbackRequest(context),
            request,
            cancellationToken);
    }
    
    [TelegramCallbackRoute(TelegramRoutes.UpdateMessageText, RouteMethod.Post)]
    public Task UpdateMessageContent(
        [FromQuery] HandleChangeContentTelegramRequest request,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        return telegramMessageService.HandleChangeContent(
            ReplyData.FromCallbackRequest(context),
            request,
            cancellationToken);
    }
}