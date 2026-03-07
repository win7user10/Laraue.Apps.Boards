using Laraue.Apps.StructuredMessages.Services;
using Laraue.Apps.StructuredMessages.TelegramServices.Services;
using Laraue.Apps.StructuredMessages.TelegramServices.Services.Messages;
using Laraue.Telegram.NET.Authentication.Services;
using Laraue.Telegram.NET.Core.Extensions;
using Laraue.Telegram.NET.Interceptors.Services;
using Telegram.Bot;

namespace Laraue.Apps.StructuredMessages.TelegramServices.Interceptors;

public class CreateCategoryFromMessageInterceptor(
    TelegramRequestContext<Guid> requestContext,
    IInterceptorState<Guid> interceptorState,
    IMessageCategoryService messageCategoryService,
    ITelegramMessageService telegramMessageService,
    ITelegramBotClient client)
    : BaseRequestInterceptor<Guid, string, CreateCategoryFromMessageInterceptorContext>(
        requestContext,
        interceptorState)
{
    protected override Task ValidateAsync(
        TelegramRequestContext<Guid> requestContext,
        InterceptResult<string> interceptResult,
        CreateCategoryFromMessageInterceptorContext fromMessageInterceptorContext,
        CancellationToken cancellationToken = default)
    {
        var text = requestContext.Update.Message?.Text;
        
        switch (text?.Length)
        {
            case null:
                interceptResult.SetError("Text message was excepted");
                break;
            case 0:
                interceptResult.SetError("Category name should contain 1 symbol at least");
                break;
            case > 128:
                interceptResult.SetError("Category name should be less than 128 symbols");
                break;
            default:
                interceptResult.SetResult(text);
                break;
        }
        
        return Task.CompletedTask;
    }

    protected override async Task<ExecutionState> ExecuteRouteAsync(
        TelegramRequestContext<Guid> requestContext,
        string model,
        CreateCategoryFromMessageInterceptorContext fromMessageInterceptorContext,
        CancellationToken cancellationToken = default)
    {
        await messageCategoryService.CreateMessageCategory(
            new CreateMessageCategoryRequest
            {
                Name = model,
                UserId = requestContext.UserId,
            }, cancellationToken);

        await client
            .SendMessage(
                requestContext.Update.GetUserId(),
                $"Category created: '{model}'",
                cancellationToken: cancellationToken);

        await telegramMessageService
            .SendMessageSaved(
                new SendMessageSavedRequest
                {
                    UserId = requestContext.UserId,
                    From = fromMessageInterceptorContext.From,
                    MessageId = fromMessageInterceptorContext.MessageId,
                    TelegramMessageId = fromMessageInterceptorContext.TelegramMessageId,
                    TelegramUserId = requestContext.Update.GetUserId(),
                },
                cancellationToken);

        return ExecutionState.FullyExecuted;
    }

    public override string Id => "CreateCategory";
}

public class CreateCategoryFromMessageInterceptorContext
{
    public required long MessageId { get; set; }
    public required int TelegramMessageId { get; set; }
    public required string? From { get; set; }
}