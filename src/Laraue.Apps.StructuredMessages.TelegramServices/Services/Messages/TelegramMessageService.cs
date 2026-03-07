using Laraue.Apps.StructuredMessages.Services;
using Laraue.Apps.StructuredMessages.TelegramServices.Interceptors;
using Laraue.Telegram.NET.Core.Extensions;
using Laraue.Telegram.NET.Core.Routing;
using Laraue.Telegram.NET.Core.Utils;
using Laraue.Telegram.NET.Interceptors.Services;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace Laraue.Apps.StructuredMessages.TelegramServices.Services.Messages;

public class TelegramMessageService(
    IMessageService messageService,
    IMessageCategoryService messageCategoryService,
    ITelegramBotClient client,
    IInterceptorState<Guid> interceptorState)
    : ITelegramMessageService
{
    public async Task HandleSaveMessage(
        SaveMessageTelegramRequest request,
        CancellationToken cancellationToken)
    {
        var id = await messageService.SaveMessage(
            new SaveMessageRequest
            {
                UserId = request.UserId,
                Text = request.Text,
                CreatedAt = request.SentAt,
            },
            cancellationToken);

        await SendMessageSaved(
            new SendMessageSavedRequest
            {
                UserId = request.UserId,
                MessageId = id,
                From = request.From,
                TelegramMessageId = request.TelegramMessageId,
                TelegramUserId = request.TelegramUserId,
            },
            cancellationToken);
    }

    public async Task HandleUpdateMessageCategory(
        ReplyData replyData,
        UpdateMessageCategoryTelegramRequest request,
        CancellationToken cancellationToken)
    {
        await messageService.UpdateMessageCategory(
            new UpdateMessageCategoryRequest
            {
                UserId = replyData.UserId,
                CategoryId = request.CategoryId,
                Id = request.Id,
            },
            cancellationToken);

        var category = await messageCategoryService
            .GetMessageCategory(request.CategoryId, cancellationToken);
        
        var tmb = new TelegramMessageBuilder()
            .AppendRow($"Selected: {category.Name}")
            .AppendRow()
            .Append("Now choose status:");

        tmb.AddInlineKeyboardButtons([InlineKeyboardButton.WithCallbackData("⏭️ Skip")]);
        
        await client.EditMessageTextAsync(
            replyData,
            tmb,
            cancellationToken: cancellationToken);
    }

    public async Task HandleCreateCategory(
        HandleCreateCategoryFromMessageRequest fromMessageRequest,
        CancellationToken cancellationToken)
    {
        var tmb = new TelegramMessageBuilder()
            .Append("Send category name:");

        await client.SendTextMessageAsync(
            fromMessageRequest.TelegramUserId,
            tmb,
            cancellationToken: cancellationToken);

        await interceptorState
            .SetAsync<CreateCategoryFromMessageInterceptor, CreateCategoryFromMessageInterceptorContext>(
                fromMessageRequest.UserId,
                new CreateCategoryFromMessageInterceptorContext
                {
                    MessageId = fromMessageRequest.TelegramMessageId,
                    From = fromMessageRequest.From,
                    TelegramMessageId = fromMessageRequest.TelegramMessageId,
                },
                cancellationToken);
    }

    public async Task SendMessageSaved(SendMessageSavedRequest request, CancellationToken cancellationToken)
    {
        var categories = await messageCategoryService.GetMessageCategories(
            request.UserId,
            cancellationToken);
        
        var tmb = new TelegramMessageBuilder()
            .Append("📥 Message saved");

        if (request.From is not null)
            tmb.Append($" from: @{request.From}");
        
        tmb
            .AppendRow()
            .AppendRow()
            .AppendRow("Choose category:");

        if (categories.Length > 0)
        {
            foreach (var typesChunked in categories.Chunk(2))
            {
                var buttons = typesChunked
                    .Select(x => 
                        new CallbackRoutePath(TelegramRoutes.SetMessageCategory)
                            .WithQueryParameter(ParameterNames.Id, request.MessageId)
                            .WithQueryParameter(ParameterNames.CategoryId, x.Id)
                            .ToInlineKeyboardButton(x.Name));
            
                tmb.AddInlineKeyboardButtons(buttons);
            }
        }

        tmb.AddInlineKeyboardButtons([
            new CallbackRoutePath(TelegramRoutes.CreateCategoryFromMessage, RouteMethod.Post)
                .WithQueryParameter(ParameterNames.TelegramMessageId,  request.TelegramMessageId)
                .ToInlineKeyboardButton("➕ New")
        ]);
        
        await client.SendTextMessageAsync(
            request.TelegramUserId,
            tmb,
            replyParameters: new ReplyParameters
            {
                MessageId = request.TelegramMessageId,
            },
            cancellationToken: cancellationToken);
    }
}