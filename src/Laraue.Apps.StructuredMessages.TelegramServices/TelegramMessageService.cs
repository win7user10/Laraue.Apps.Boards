using Laraue.Apps.StructuredMessages.Services;
using Laraue.Telegram.NET.Abstractions.Request;
using Laraue.Telegram.NET.Core.Extensions;
using Laraue.Telegram.NET.Core.Routing;
using Laraue.Telegram.NET.Core.Utils;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Laraue.Apps.StructuredMessages.TelegramServices;

public interface ITelegramMessageService
{
    Task HandleSaveMessage(
        SaveMessageTelegramRequest request,
        CancellationToken cancellationToken);
    
    Task HandleUpdateMessageCategory(
        ReplyData replyData,
        UpdateMessageCategoryTelegramRequest request,
        CancellationToken cancellationToken);
}

public class TelegramMessageService(
    IMessageService messageService,
    IMessageCategoryService messageCategoryService,
    ITelegramBotClient client)
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

        var categories = await messageCategoryService.GetMessageCategories(
            request.UserId,
            cancellationToken);

        var tmb = new TelegramMessageBuilder()
            .Append($"The message saved with id: {id}");

        if (categories.Length > 0)
        {
            tmb
                .AppendRow()
                .AppendRow("Select the category for message.");

            foreach (var typesChunked in categories.Chunk(2))
            {
                var buttons = typesChunked
                    .Select(x => 
                        new CallbackRoutePath(TelegramRoutes.SetMessageCategory)
                            .WithQueryParameter(ParameterNames.Id, id)
                            .WithQueryParameter(ParameterNames.CategoryId, x.Id)
                            .ToInlineKeyboardButton(x.Name));
            
                tmb.AddInlineKeyboardButtons(buttons);
            }
        }
        
        await client.SendTextMessageAsync(
            request.TelegramUserId,
            tmb,
            replyParameters: new ReplyParameters
            {
                MessageId = request.MessageId,
            },
            cancellationToken: cancellationToken);
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
        
        var tmb = new TelegramMessageBuilder()
            .Append($"The category has been updated to: {request.CategoryId}");
        
        await client.EditMessageTextAsync(
            replyData,
            tmb,
            cancellationToken: cancellationToken);
    }
}

public class SaveMessageTelegramRequest
{
    public required long TelegramUserId { get; set; }
    public required int MessageId { get; set; }
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