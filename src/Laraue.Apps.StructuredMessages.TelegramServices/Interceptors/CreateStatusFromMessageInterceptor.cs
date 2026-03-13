using Laraue.Apps.StructuredMessages.Services;
using Laraue.Apps.StructuredMessages.TelegramServices.Services.Messages;
using Laraue.Telegram.NET.Authentication.Services;
using Laraue.Telegram.NET.Core.Extensions;
using Laraue.Telegram.NET.Interceptors.Services;
using Telegram.Bot;

namespace Laraue.Apps.StructuredMessages.TelegramServices.Interceptors;

public class CreateStatusFromMessageInterceptor(
    TelegramRequestContext<Guid> requestContext,
    IInterceptorState<Guid> interceptorState,
    ICoreStatusService coreStatusService,
    ICoreCategoryService categoryService,
    ITelegramMessageService telegramMessageService,
    ITelegramBotClient client)
    : BaseRequestInterceptor<Guid, string, CreateStatusFromMessageInterceptorContext>(
        requestContext,
        interceptorState)
{
    protected override Task ValidateAsync(
        TelegramRequestContext<Guid> requestContext,
        InterceptResult<string> interceptResult,
        CreateStatusFromMessageInterceptorContext fromMessageInterceptorContext,
        CancellationToken cancellationToken = default)
    {
        var text = requestContext.Update.Message?.Text;
        
        switch (text?.Length)
        {
            case null:
                interceptResult.SetError("Text message was excepted");
                break;
            case 0:
                interceptResult.SetError("Status name should contain 1 symbol at least");
                break;
            case > 128:
                interceptResult.SetError("Status name should be less than 128 symbols");
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
        CreateStatusFromMessageInterceptorContext interceptorContext,
        CancellationToken cancellationToken = default)
    {
        if (!await categoryService.UserHasAccessToCategory(
            requestContext.UserId,
            interceptorContext.MessageCategoryId,
            cancellationToken))
        {
            await client.SendMessage(
                requestContext.Update.GetUserId(),
                "Unable to save status. Category was not found.",
                cancellationToken: cancellationToken);
            
            return ExecutionState.FullyExecuted;
        }
            
        await coreStatusService.Create(
            new CreateMessageCategoryStatusRequest
            {
                Name = model,
                CategoryId = interceptorContext.MessageCategoryId,
            }, cancellationToken);

        await telegramMessageService.OpenChangeStatusWindow(
            requestContext.UserId,
            editMessageId: null,
            new HandleOpenChangeStatusWindowRequest
            {
                MessageId = interceptorContext.MessageId,
            },
            cancellationToken);

        return ExecutionState.FullyExecuted;
    }

    public override string Id => "CreateCategoryStatus";
}

public class CreateStatusFromMessageInterceptorContext
{
    public required long MessageId { get; set; }
    public required int TelegramMessageId { get; set; }
    public required long MessageCategoryId { get; set; }
}