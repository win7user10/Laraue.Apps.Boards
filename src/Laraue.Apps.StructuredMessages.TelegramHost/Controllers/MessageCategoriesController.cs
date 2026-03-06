using Laraue.Apps.StructuredMessages.TelegramServices;
using Laraue.Telegram.NET.Abstractions.Request;
using Laraue.Telegram.NET.Core.Routing;
using Laraue.Telegram.NET.Core.Routing.Attributes;

namespace Laraue.Apps.StructuredMessages.TelegramHost.Controllers;

public class MessageCategoriesController(ITelegramMessageCategoryService messageCategoryService)
    : TelegramController
{
    [TelegramMessageRoute(TelegramRoutes.Categories)]
    public Task HandleGetCategories(
        RequestContext requestContext,
        CancellationToken cancellationToken)
    {
        return messageCategoryService.HandleGetCategories(
            ReplyData.FromMessageRequest(requestContext),
            cancellationToken);
    }
    
    [TelegramMessageRoute(TelegramRoutes.CreateCategory)]
    public Task CreateCategory(
        RequestContext requestContext,
        [FromPath] string name,
        CancellationToken cancellationToken)
    {
        return messageCategoryService.HandleCreateCategory(
            ReplyData.FromMessageRequest(requestContext),
            new CreateMessageCategoryTelegramRequest
            {
                Name = name,
                MessageId = requestContext.Update.Message!.Id,
            },
            cancellationToken);}
}