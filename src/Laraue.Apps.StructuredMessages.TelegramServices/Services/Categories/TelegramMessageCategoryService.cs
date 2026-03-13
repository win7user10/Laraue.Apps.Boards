using Laraue.Apps.StructuredMessages.Services;
using Laraue.Telegram.NET.Core.Extensions;
using Laraue.Telegram.NET.Core.Routing;
using Laraue.Telegram.NET.Core.Utils;
using Laraue.Telegram.NET.Interceptors.Services;
using Telegram.Bot;

namespace Laraue.Apps.StructuredMessages.TelegramServices.Services.Categories;

public class TelegramMessageCategoryService(
    ICoreCategoryService coreCategoryService,
    ITelegramBotClient client,
    IInterceptorState<Guid> interceptorState)
    : ITelegramMessageCategoryService
{
    public async Task HandleGetCategories(
        ReplyData reply,
        CancellationToken cancellationToken)
    {
        var categories = await coreCategoryService.GetList(
            reply.UserId,
            cancellationToken);

        var tmb = new TelegramMessageBuilder()
            .Append("📋 Your Categories:");
        
        foreach (var typesChunked in categories.Chunk(2))
        {
            var buttons = typesChunked
                .Select(x => 
                    new CallbackRoutePath(TelegramRoutes.Category)
                        .WithQueryParameter(ParameterNames.MessageCategoryId, x.Id)
                        .ToInlineKeyboardButton(x.Name));
            
            tmb.AddInlineKeyboardButtons(buttons);
        }

        tmb.AddInlineKeyboardButtons([
            new CallbackRoutePath(TelegramRoutes.CreateCategoryFromMessage, RouteMethod.Post)
                .ToInlineKeyboardButton("➕ New")
        ]);

        await client.SendTextMessageAsync(
            reply.TelegramId,
            tmb,
            cancellationToken: cancellationToken);
    }
}