using Laraue.Apps.StructuredMessages.TelegramServices;
using Laraue.Apps.StructuredMessages.TelegramServices.Services.Categories;
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
}