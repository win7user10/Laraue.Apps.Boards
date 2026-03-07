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
    IMessageStatusService messageStatusService,
    ITelegramBotClient client,
    IInterceptorState<Guid> interceptorState,
    ITelegramMessageServiceRepository repository)
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
                Sender = request.From,
                TelegramMessageId = request.TelegramMessageId,
            },
            cancellationToken);

        await SendMessageToChat(id, cancellationToken);
    }

    public Task HandleUpdateMessageCategory(
        ReplyData replyData,
        UpdateMessageCategoryTelegramRequest request,
        CancellationToken cancellationToken)
    {
        return UpdateMessageIfPermitted(
            replyData.UserId,
            request.MessageId,
            () =>
            {
                return messageService.UpdateMessage(
                    request.MessageId,
                    setters => setters
                        .SetProperty(x => x.CategoryId, request.CategoryId),
                    cancellationToken);
            }, cancellationToken);
    }

    public async Task HandleCreateCategory(
        HandleCreateCategoryFromMessageRequest request,
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
                    MessageId = request.MessageId,
                },
                cancellationToken);
    }
    
    public async Task HandleChangeContent(
        ReplyData replyData,
        HandleChangeContentTelegramRequest request,
        CancellationToken cancellationToken)
    {
        var tmb = new TelegramMessageBuilder()
            .Append("Send new text. The previous text can be copied from the previous message:");

        await client.SendTextMessageAsync(
            replyData.TelegramId,
            tmb,
            cancellationToken: cancellationToken);

        await interceptorState
            .SetAsync<ChangeMessageTextInterceptor, ChangeMessageTextInterceptorContext>(
                replyData.UserId,
                new ChangeMessageTextInterceptorContext
                {
                    MessageId = request.MessageId,
                },
                cancellationToken);
    }

    public Task HandleUpdateStatus(
        ReplyData replyData,
        UpdateMessageStatusTelegramRequest request,
        CancellationToken cancellationToken)
    {
        return UpdateMessageIfPermitted(
            replyData.UserId,
            request.MessageId,
            async () =>
            {
                if (!await messageStatusService.UserHasAccessToStatus(
                    replyData.UserId,
                    request.StatusId,
                    cancellationToken))
                {
                    // TODO - status deleted or no access?
                    return;
                }
                
                await messageService.UpdateMessage(
                    request.MessageId,
                    setters => setters
                        .SetProperty(x => x.StatusId, request.StatusId),
                    cancellationToken);
                
            }, cancellationToken);
    }

    public async Task HandleCreateStatus(
        HandleCreateStatusFromMessageRequest request,
        CancellationToken cancellationToken)
    {
        var tmb = new TelegramMessageBuilder()
            .Append("Send status name:");

        await client.SendTextMessageAsync(
            request.TelegramUserId,
            tmb,
            cancellationToken: cancellationToken);

        await interceptorState
            .SetAsync<CreateStatusFromMessageInterceptor, CreateStatusFromMessageInterceptorContext>(
                request.UserId,
                new CreateStatusFromMessageInterceptorContext
                {
                    MessageId = request.MessageId,
                    MessageCategoryId = request.MessageCategoryId,
                },
                cancellationToken);
    }

    public async Task SendMessageToChat(
        long messageId,
        CancellationToken cancellationToken)
    {
        var message = await repository.GetMessage(
            messageId,
            cancellationToken);

        if (message.CategoryId is null)
        {
            await SendMessageWithCategoriesToChat(
                message,
                cancellationToken);

            return;
        }

        if (message.StatusId is null)
        {
            await SendMessageWithStatusesToChat(
                message,
                cancellationToken);
            
            return;
        }
        
        await SendReadyMessageToChat(
            message,
            cancellationToken);
    }

    private async Task UpdateMessageIfPermitted(
        Guid userId,
        long messageId,
        Func<Task> executeUpdate,
        CancellationToken cancellationToken)
    {
        if (!await messageService.UserHasAccessToMessage(
                userId,
                messageId,
                cancellationToken))
        {
            // TODO - message deleted or no access?
            return;
        }

        await executeUpdate();

        await SendMessageToChat(
            messageId,
            cancellationToken);
    }
    
    private async Task SendReadyMessageToChat(
        MessageDto message,
        CancellationToken cancellationToken)
    {
        var tmb = GetTitleBuilder("✅", message);

        tmb.AddInlineKeyboardButtons([
            InlineKeyboardButton.WithCopyText(
                "Copy",
                message.Content),
            new CallbackRoutePath(TelegramRoutes.UpdateMessageText, RouteMethod.Post)
                .WithQueryParameters(new HandleChangeContentTelegramRequest
                {
                    MessageId = message.Id
                })
                .ToInlineKeyboardButton("Edit")
        ]);

        await client.SendTextMessageAsync(
            message.UserTelegramId,
            tmb,
            cancellationToken: cancellationToken);
    }
    
    private async Task SendMessageWithCategoriesToChat(
        MessageDto message,
        CancellationToken cancellationToken)
    {
        var categories = await messageCategoryService.GetMessageCategories(
            message.UserId,
            cancellationToken);

        var tmb = GetTitleBuilder("📥", message)
            .AppendRow("Choose category:");

        if (categories.Length > 0)
        {
            foreach (var typesChunked in categories.Chunk(2))
            {
                var buttons = typesChunked
                    .Select(x => 
                        new CallbackRoutePath(TelegramRoutes.SetMessageCategory)
                            .WithQueryParameters(new UpdateMessageCategoryTelegramRequest
                            {
                                MessageId = message.Id,
                                CategoryId = x.Id,
                            })
                            .ToInlineKeyboardButton(x.Name));
            
                tmb.AddInlineKeyboardButtons(buttons);
            }
        }

        tmb.AddInlineKeyboardButtons([
            new CallbackRoutePath(TelegramRoutes.CreateCategoryFromMessage, RouteMethod.Post)
                .WithQueryParameter(ParameterNames.MessageId, message.Id)
                .ToInlineKeyboardButton("➕ New")
        ]);

        await client.SendTextMessageAsync(
            message.UserTelegramId,
            tmb,
            cancellationToken: cancellationToken);
    }
    
    private async Task SendMessageWithStatusesToChat(
        MessageDto message,
        CancellationToken cancellationToken)
    {
        var statuses = await messageStatusService.GetStatuses(
            message.UserId,
            cancellationToken);
        
        var tmb = GetTitleBuilder("📥", message)
            .AppendRow("Choose initial status:");

        if (statuses.Length > 0)
        {
            foreach (var typesChunked in statuses.Chunk(2))
            {
                var buttons = typesChunked
                    .Select(x => 
                        new CallbackRoutePath(TelegramRoutes.SetMessageStatus)
                            .WithQueryParameters(new UpdateMessageStatusTelegramRequest
                            {
                                MessageId = message.Id,
                                StatusId = x.Id,
                            })
                            .ToInlineKeyboardButton(x.Name));
            
                tmb.AddInlineKeyboardButtons(buttons);
            }
        }

        tmb.AddInlineKeyboardButtons([
            new CallbackRoutePath(TelegramRoutes.CreateStatusFromMessage, RouteMethod.Post)
                .WithQueryParameters(new CreateStatusFromMessageTelegramRequest
                {
                    MessageId = message.Id,
                    MessageCategoryId = message.CategoryId.GetValueOrDefault(),
                })
                .ToInlineKeyboardButton("➕ New")
        ]);

        await client.SendTextMessageAsync(
            message.UserTelegramId,
            tmb,
            cancellationToken: cancellationToken);
    }

    private static TelegramMessageBuilder GetTitleBuilder(string icon, MessageDto message)
    {
        var tmb = new TelegramMessageBuilder()
            .Append($"{icon} Message saved");

        if (message.Sender is not null)
            tmb.Append($" from: @{message.Sender}");

        if (message.CategoryName is not null)
            tmb
                .AppendRow()
                .AppendRow()
                .Append($"Category: {message.CategoryName}");

        if (message.StatusName is not null)
            tmb.Append($" -> {message.StatusName}");

        tmb
            .AppendRow()
            .AppendRow()
            .Append("Message: ")
            .AppendRow(message.Content)
            .AppendRow();
        
        return tmb;
    }
}