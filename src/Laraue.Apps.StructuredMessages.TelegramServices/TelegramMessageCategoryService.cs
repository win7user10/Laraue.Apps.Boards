using Laraue.Apps.StructuredMessages.Services;
using Laraue.Telegram.NET.Core.Extensions;
using Laraue.Telegram.NET.Core.Routing;
using Laraue.Telegram.NET.Core.Utils;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace Laraue.Apps.StructuredMessages.TelegramServices;

public interface ITelegramMessageCategoryService
{
    Task HandleGetCategories(
        ReplyData reply,
        CancellationToken cancellationToken);
    
    Task HandleCreateCategory(
        ReplyData reply,
        CreateMessageCategoryTelegramRequest request,
        CancellationToken cancellationToken);
}

public class TelegramMessageCategoryService(
    IMessageCategoryService messageCategoryService,
    ITelegramBotClient client)
    : ITelegramMessageCategoryService
{
    public async Task HandleGetCategories(
        ReplyData reply,
        CancellationToken cancellationToken)
    {
        var categories = await messageCategoryService.GetMessageCategories(
            reply.UserId,
            cancellationToken);

        var tmb = new TelegramMessageBuilder()
            .Append("📋 Your Categories:");
        
        foreach (var typesChunked in categories.Chunk(2))
        {
            var buttons = typesChunked
                .Select(x => 
                    new CallbackRoutePath(TelegramRoutes.Category)
                        .WithQueryParameter(ParameterNames.Id, x.Id)
                        .ToInlineKeyboardButton(x.Name));
            
            tmb.AddInlineKeyboardButtons(buttons);
        }

        tmb.AddInlineKeyboardButtons([InlineKeyboardButton.WithSwitchInlineQueryCurrentChat(
            "➕ New Category", $"{TelegramRoutes.CreateCategory} ")]);

        await client.SendTextMessageAsync(
            reply.TelegramId,
            tmb,
            cancellationToken: cancellationToken);
    }

    public async Task HandleCreateCategory(
        ReplyData reply,
        CreateMessageCategoryTelegramRequest request,
        CancellationToken cancellationToken)
    {
        var categoryId = await messageCategoryService.CreateMessageCategory(
            new CreateMessageCategoryRequest
            {
                Name = request.Name,
                UserId = reply.UserId,
            },
            cancellationToken);
        
        var tmb = new TelegramMessageBuilder()
            .Append($"Category created ({categoryId}): {request.Name}");

        await client.SendTextMessageAsync(
            reply.TelegramId,
            tmb,
            replyParameters: new ReplyParameters
            {
                MessageId = request.MessageId,
            },
            cancellationToken: cancellationToken);
    }
}

public class CreateMessageCategoryTelegramRequest
{
    public required string Name { get; set; }
    public required int MessageId { get; set; }
}