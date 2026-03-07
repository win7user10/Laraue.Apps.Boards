using Laraue.Apps.StructuredMessages.Services;
using Laraue.Apps.StructuredMessages.TelegramServices.Interceptors;
using Laraue.Telegram.NET.Abstractions.Request;
using Laraue.Telegram.NET.Core.Extensions;
using Laraue.Telegram.NET.Core.Routing;
using Laraue.Telegram.NET.Core.Utils;
using Laraue.Telegram.NET.Interceptors.Services;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace Laraue.Apps.StructuredMessages.TelegramServices.Services;

public interface ITelegramMessageService
{
    Task HandleSaveMessage(
        SaveMessageTelegramRequest request,
        CancellationToken cancellationToken);
    
    Task HandleUpdateMessageCategory(
        ReplyData replyData,
        UpdateMessageCategoryTelegramRequest request,
        CancellationToken cancellationToken);
    
    Task HandleCreateCategory(
        HandleCreateCategoryRequest request,
        CancellationToken cancellationToken);
    
    Task SendMessageSaved(
        SendMessageSavedRequest request,
        CancellationToken cancellationToken);
}

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
        HandleCreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var tmb = new TelegramMessageBuilder()
            .Append("Send category name:");

        await client.SendTextMessageAsync(
            request.TelegramUserId,
            tmb,
            cancellationToken: cancellationToken);

        await interceptorState
            .SetAsync<CreateCategoryFromMessageInterceptor, CreateCategoryFromMessageInterceptorContext>(
                request.UserId,
                new CreateCategoryFromMessageInterceptorContext
                {
                    MessageId = request.TelegramMessageId,
                    From = request.From,
                    TelegramMessageId = request.TelegramMessageId,
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

public class SaveMessageTelegramRequest
{
    public required string? From { get; set; }
    public required long TelegramUserId { get; set; }
    public required int TelegramMessageId { get; set; }
    public required Guid UserId { get; set; }
    public required string Text { get; set; }
    public required DateTime SentAt { get; set; }
}

public class UpdateMessageCategoryTelegramRequest
{
    [FromQuery(ParameterNames.Id)]
    public required long Id { get; set; }
    
    [FromQuery(ParameterNames.CategoryId)]
    public required long CategoryId { get; set; }
}

public class CreateCategoryFromMessageTelegramRequest
{
    [FromQuery(ParameterNames.TelegramMessageId)]
    public required int MessageId { get; set; }
}

public class SendMessageSavedRequest
{
    public required string? From { get; set; }
    public required long TelegramUserId { get; set; }
    public required int TelegramMessageId { get; set; }
    public required long MessageId { get; set; }
    public required Guid UserId { get; set; }
}

public class HandleCreateCategoryRequest
{
    public required string? From { get; set; }
    public required long TelegramUserId { get; set; }
    public required int TelegramMessageId { get; set; }
    public required Guid UserId { get; set; }
}