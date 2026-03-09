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

        await UpdateMessageInChat(
            id,
            editMessageId: null,
            cancellationToken);
    }

    public Task HandleUpdateMessageCategory(
        ReplyData replyData,
        UpdateMessageCategoryTelegramRequest request,
        CancellationToken cancellationToken)
    {
        return UpdateMessageIfPermitted(
            replyData.UserId,
            request.MessageId,
            replyData.MessageId,
            _ =>
            {
                return messageService.UpdateMessage(
                    request.MessageId,
                    setters => setters
                        .SetProperty(x => x.CategoryId, request.CategoryId)
                        .SetProperty(x => x.StatusId, (long?)null),
                    cancellationToken);
            }, cancellationToken);
    }

    public async Task OpenChangeCategoryWindow(
        Guid userId,
        int? editMessageId,
        HandleOpenChangeCategoryWindowRequest request,
        CancellationToken cancellationToken)
    {
        if (!await messageService.UserHasAccessToMessage(
            userId,
            request.MessageId,
            cancellationToken))
        {
            // TODO - message deleted or no access?
            return;
        }

        var message = await repository.GetMessage(
            request.MessageId,
            cancellationToken);
        
        var categories = await messageCategoryService.GetMessageCategories(
            message.UserId,
            cancellationToken);

        var tmb = new TelegramMessageBuilder()
            .AppendRow("Choose category:");

        if (categories.Length > 0)
        {
            foreach (var typesChunked in categories.Chunk(2))
            {
                var buttons = typesChunked
                    .Select(x => 
                        new CallbackRoutePath(TelegramRoutes.UpdateMessageCategory, RouteMethod.Post)
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
                .ToInlineKeyboardButton("➕ New Category")
        ]);

        await SendOrEditMessage(
            editMessageId,
            message.TelegramMessageId,
            message.UserTelegramId,
            tmb,
            cancellationToken);
    }

    public async Task HandleCreateCategory(
        HandleCreateCategoryFromMessageRequest request,
        CancellationToken cancellationToken)
    {
        var tmb = new TelegramMessageBuilder()
            .Append("Send category name:");

        await client.SendTextMessageAsync(
            request.ReplyData.TelegramId,
            tmb,
            cancellationToken: cancellationToken);

        await interceptorState
            .SetAsync<CreateCategoryFromMessageInterceptor, CreateCategoryFromMessageInterceptorContext>(
                request.ReplyData.UserId,
                new CreateCategoryFromMessageInterceptorContext
                {
                    MessageId = request.MessageId,
                },
                cancellationToken);
    }

    public async Task OpenChangeStatusWindow(
        Guid userId,
        int? editMessageId,
        HandleOpenChangeStatusWindowRequest request,
        CancellationToken cancellationToken)
    {
        if (!await messageService.UserHasAccessToMessage(
            userId,
            request.MessageId,
            cancellationToken))
        {
            // TODO - message deleted or no access?
            return;
        }

        var message = await repository.GetMessage(
            request.MessageId,
            cancellationToken);
        
        var statuses = await messageStatusService.GetStatuses(
            message.CategoryId.GetValueOrDefault(),
            cancellationToken);
                
        var tmb = new TelegramMessageBuilder()
            .AppendRow("Choose status:");
        
        foreach (var typesChunked in statuses.Chunk(2))
        {
            var buttons = typesChunked
                .Select(x => 
                    new CallbackRoutePath(TelegramRoutes.UpdateMessageStatus, RouteMethod.Post)
                        .WithQueryParameters(new UpdateMessageStatusTelegramRequest
                        {
                            MessageId = message.Id,
                            StatusId = x.Id,
                        })
                        .ToInlineKeyboardButton(x.Name));
            
            tmb.AddInlineKeyboardButtons(buttons);
        }

        tmb.AddInlineKeyboardButtons([
            new CallbackRoutePath(TelegramRoutes.CreateStatusFromMessage, RouteMethod.Post)
                .WithQueryParameters(new CreateStatusFromMessageTelegramRequest
                {
                    MessageId = message.Id,
                    MessageCategoryId = message.CategoryId.GetValueOrDefault(),
                })
                .ToInlineKeyboardButton("➕ New Status")
        ]);

        await SendOrEditMessage(
            editMessageId,
            message.TelegramMessageId,
            message.UserTelegramId,
            tmb,
            cancellationToken);
    }

    public Task HandleChangeContent(
        ReplyData replyData,
        HandleChangeContentTelegramRequest request,
        CancellationToken cancellationToken)
    {
        return UpdateMessageIfPermitted(
            replyData.UserId,
            request.MessageId,
            replyData.MessageId,
            async _ =>
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
                
            }, cancellationToken);
    }

    public Task HandleUpdateStatus(
        ReplyData replyData,
        UpdateMessageStatusTelegramRequest request,
        CancellationToken cancellationToken)
    {
        return UpdateMessageIfPermitted(
            replyData.UserId,
            request.MessageId,
            replyData.MessageId,
            async _ =>
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
            request.ReplyData.TelegramId,
            tmb,
            cancellationToken: cancellationToken);

        await interceptorState
            .SetAsync<CreateStatusFromMessageInterceptor, CreateStatusFromMessageInterceptorContext>(
                request.ReplyData.UserId,
                new CreateStatusFromMessageInterceptorContext
                {
                    MessageId = request.MessageId,
                    MessageCategoryId = request.MessageCategoryId,
                    TelegramMessageId = request.ReplyData.MessageId,
                },
                cancellationToken);
    }

    public async Task UpdateMessageInChat(
        long messageId,
        int? editMessageId,
        CancellationToken cancellationToken)
    {
        var message = await repository.GetMessage(
            messageId,
            cancellationToken);

        await SendReadyMessageToChat(
            message,
            editMessageId,
            cancellationToken);
    }

    private async Task UpdateMessageIfPermitted(
        Guid userId,
        long messageId,
        int telegramMessageId,
        Func<MessageDto, Task> executeUpdate,
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
        
        var message = await repository.GetMessage(
            messageId,
            cancellationToken);

        await executeUpdate(message);

        await UpdateMessageInChat(
            messageId,
            telegramMessageId,
            cancellationToken);
    }
    
    private async Task SendReadyMessageToChat(
        MessageDto message,
        int? editMessageId,
        CancellationToken cancellationToken)
    {
        var tmb = new TelegramMessageBuilder()
            .Append(message.Content);
        
        tmb.AddInlineKeyboardButtons([
            new CallbackRoutePath(TelegramRoutes.UpdateMessageCategory)
                .WithQueryParameters(new HandleOpenChangeCategoryWindowRequest
                {
                    MessageId = message.Id
                })
                .ToInlineKeyboardButton($"Category: {message.CategoryName ?? "Not set"}")
        ]);
        
        if (message.CategoryId is not null)
            tmb.AddInlineKeyboardButtons([
                new CallbackRoutePath(TelegramRoutes.UpdateMessageStatus)
                    .WithQueryParameters(new HandleOpenChangeStatusWindowRequest
                    {
                        MessageId = message.Id
                    })
                    .ToInlineKeyboardButton($"Status: {message.StatusName ?? "Not set"}")
            ]);

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

        await SendOrEditMessage(
            editMessageId,
            message.TelegramMessageId,
            message.UserTelegramId,
            tmb,
            cancellationToken);
    }

    private Task SendOrEditMessage(
        int? editMessageId,
        int? replyMessageId,
        long userTelegramId,
        TelegramMessageBuilder tmb,
        CancellationToken cancellationToken)
    {
        if (editMessageId is not null)
        {
            return client.EditMessageTextAsync(
                userTelegramId,
                editMessageId.Value,
                tmb,
                cancellationToken: cancellationToken);
        }

        if (replyMessageId is not null)
        {
            return client.SendTextMessageAsync(
                userTelegramId,
                tmb,
                replyParameters: new ReplyParameters
                {
                    MessageId = replyMessageId.Value,
                    AllowSendingWithoutReply = true,
                },
                cancellationToken: cancellationToken);
        }
        
        return Task.CompletedTask;
    }
}