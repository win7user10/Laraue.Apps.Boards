using Laraue.Apps.StructuredMessages.Services;
using Laraue.Apps.StructuredMessages.TelegramServices.Interceptors;
using Laraue.Apps.StructuredMessages.TelegramServices.Resources;
using Laraue.Telegram.NET.Core.Extensions;
using Laraue.Telegram.NET.Core.Routing;
using Laraue.Telegram.NET.Core.Utils;
using Laraue.Telegram.NET.Interceptors.Services;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace Laraue.Apps.StructuredMessages.TelegramServices.Services.Messages;

public class TelegramMessageService(
    ICoreMessageService messageService,
    ICoreCategoryService coreCategoryService,
    ICoreStatusService coreStatusService,
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
                TelegramMessageId = request.TelegramMessageId,
            },
            cancellationToken);

        await OpenChangeCategoryWindow(
            request.UserId,
            editMessageId: null,
            new HandleOpenChangeCategoryWindowRequest
            {
                MessageId = id
            },
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
            _ => messageService.UpdateCategory(
                request.MessageId,
                request.CategoryId,
                cancellationToken), cancellationToken);
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
        
        var categories = await coreCategoryService.GetList(
            message.UserId,
            cancellationToken);

        var tmb = new TelegramMessageBuilder()
            .AppendRow($"{Phrases.ChooseCategory}:");

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
                .ToInlineKeyboardButton($"➕ {Phrases.NewCategory}")
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
            .Append($"{Phrases.SendCategoryName}:");

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
        
        var statuses = await coreStatusService.GetStatuses(
            message.CategoryId.GetValueOrDefault(),
            cancellationToken);
                
        var tmb = new TelegramMessageBuilder()
            .AppendRow($"{Phrases.ChooseStatus}:");
        
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
                .ToInlineKeyboardButton($"➕ {Phrases.NewStatus}")
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
                    .Append($"{Phrases.SendNewText}:");

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

    public async Task HandleDelete(
        ReplyData replyData,
        HandleDeleteMessageTelegramRequest request,
        CancellationToken cancellationToken)
    {
        if (!await messageService.UserHasAccessToMessage(
            replyData.UserId,
            request.MessageId,
            cancellationToken))
        {
            // TODO - message deleted or no access?
            return;
        }
        
        await messageService.DeleteMessage(request.MessageId, cancellationToken);

        var tmb = new TelegramMessageBuilder()
            .Append(Phrases.Deleted);

        await client.EditMessageTextAsync(
            replyData,
            tmb,
            cancellationToken: cancellationToken);
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
                await messageService.UpdateStatus(
                    request.MessageId,
                    request.StatusId,
                    cancellationToken);
            }, cancellationToken);
    }

    public async Task HandleCreateStatus(
        HandleCreateStatusFromMessageRequest request,
        CancellationToken cancellationToken)
    {
        var tmb = new TelegramMessageBuilder()
            .Append($"{Phrases.SendStatusName}:");

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
                .ToInlineKeyboardButton($"{Phrases.Category}: {message.CategoryName ?? Phrases.NotSet}")
        ]);
        
        if (message.CategoryId is not null)
            tmb.AddInlineKeyboardButtons([
                new CallbackRoutePath(TelegramRoutes.UpdateMessageStatus)
                    .WithQueryParameters(new HandleOpenChangeStatusWindowRequest
                    {
                        MessageId = message.Id
                    })
                    .ToInlineKeyboardButton($"{Phrases.Status}: {message.StatusName ?? Phrases.NotSet}")
            ]);

        tmb.AddInlineKeyboardButtons([
            InlineKeyboardButton.WithCopyText(
                Phrases.Copy,
                message.Content),
            new CallbackRoutePath(TelegramRoutes.UpdateMessageText, RouteMethod.Post)
                .WithQueryParameters(new HandleChangeContentTelegramRequest
                {
                    MessageId = message.Id
                })
                .ToInlineKeyboardButton(Phrases.Edit)
        ]);
        
        tmb.AddInlineKeyboardButtons([
            new CallbackRoutePath(TelegramRoutes.Message, RouteMethod.Delete)
                .WithQueryParameters(new HandleDeleteMessageTelegramRequest
                {
                    MessageId = message.Id
                })
                .ToInlineKeyboardButton(Phrases.Delete)
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