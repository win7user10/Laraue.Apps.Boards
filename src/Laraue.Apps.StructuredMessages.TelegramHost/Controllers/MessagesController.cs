using Laraue.Apps.StructuredMessages.TelegramServices;
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
                From = context.Update.GetUser()?.Username,
                MessageId = request.MessageId,
                ReplyData = ReplyData.FromCallbackRequest(context),
            },
            cancellationToken);
    }
    
    [TelegramCallbackRoute(TelegramRoutes.UpdateMessageCategory)]
    public Task OpenChangeCategoryWindow(
        [FromQuery] HandleOpenChangeCategoryWindowRequest request,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        return telegramMessageService.OpenChangeCategoryWindow(
            context.UserId,
            context.Update.CallbackQuery.GetMessageId(),
            request,
            cancellationToken);
    }
    
    [TelegramCallbackRoute(TelegramRoutes.UpdateMessageCategory, RouteMethod.Post)]
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
                MessageId = request.MessageId,
                MessageCategoryId = request.MessageCategoryId,
                ReplyData = ReplyData.FromCallbackRequest(context),
            },
            cancellationToken);
    }
    
    [TelegramCallbackRoute(TelegramRoutes.UpdateMessageStatus)]
    public Task OpenChangeStatusWindow(
        [FromQuery] HandleOpenChangeStatusWindowRequest request,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        return telegramMessageService.OpenChangeStatusWindow(
            context.UserId,
            context.Update.CallbackQuery.GetMessageId(),
            request,
            cancellationToken);
    }
    
    [TelegramCallbackRoute(TelegramRoutes.UpdateMessageStatus, RouteMethod.Post)]
    public Task UpdateMessageStatus(
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
    
    [TelegramCallbackRoute(TelegramRoutes.Message, RouteMethod.Delete)]
    public Task DeleteMessage(
        [FromQuery] HandleDeleteMessageTelegramRequest request,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        return telegramMessageService.HandleDelete(
            ReplyData.FromCallbackRequest(context),
            request,
            cancellationToken);
    }
}