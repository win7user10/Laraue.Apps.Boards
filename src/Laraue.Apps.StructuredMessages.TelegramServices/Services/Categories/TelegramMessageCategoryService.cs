using Laraue.Apps.StructuredMessages.Services;
using Laraue.Apps.StructuredMessages.TelegramServices.Resources;
using Laraue.Telegram.NET.Core.Extensions;
using Laraue.Telegram.NET.Core.Routing;
using Laraue.Telegram.NET.Core.Utils;
using Telegram.Bot;

namespace Laraue.Apps.StructuredMessages.TelegramServices.Services.Categories;

public class TelegramMessageCategoryService(
    ICoreEpicsService coreCategoryService,
    ITelegramBotClient client)
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
            .Append($"📋 {Phrases.YourCategories}:");
        
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
                .ToInlineKeyboardButton($"➕ {Phrases.NewCategory}")
        ]);

        await client.SendTextMessageAsync(
            reply.TelegramId,
            tmb,
            cancellationToken: cancellationToken);
    }
}