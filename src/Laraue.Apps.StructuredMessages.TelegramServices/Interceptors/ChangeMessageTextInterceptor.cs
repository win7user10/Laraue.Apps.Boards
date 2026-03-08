using Laraue.Apps.StructuredMessages.Services;
using Laraue.Apps.StructuredMessages.TelegramServices.Services.Messages;
using Laraue.Telegram.NET.Authentication.Services;
using Laraue.Telegram.NET.Interceptors.Services;

namespace Laraue.Apps.StructuredMessages.TelegramServices.Interceptors;

public class ChangeMessageTextInterceptor(
    TelegramRequestContext<Guid> requestContext,
    IInterceptorState<Guid> interceptorState,
    IMessageService messageService,
    ITelegramMessageService telegramMessageService)
    : BaseRequestInterceptor<Guid, string, ChangeMessageTextInterceptorContext>(
        requestContext,
        interceptorState)
{
    protected override Task ValidateAsync(
        TelegramRequestContext<Guid> requestContext,
        InterceptResult<string> interceptResult,
        ChangeMessageTextInterceptorContext interceptorContext,
        CancellationToken cancellationToken = default)
    {
        var text = requestContext.Update.Message?.Text;
        
        switch (text?.Length)
        {
            case null:
                interceptResult.SetError("Text message was excepted");
                break;
            case 0:
                interceptResult.SetError("Text should contain 1 symbol at least");
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
        ChangeMessageTextInterceptorContext interceptorContext,
        CancellationToken cancellationToken = default)
    {
        await messageService.UpdateMessage(
            interceptorContext.MessageId,
            setters => setters
                .SetProperty(x => x.Content, model),
            cancellationToken);

        await telegramMessageService
            .UpdateMessageInChat(
                interceptorContext.MessageId,
                interceptorContext.TelegramMessageId,
                cancellationToken);

        return ExecutionState.FullyExecuted;
    }

    public override string Id => "ChangeMessageText";
}

public class ChangeMessageTextInterceptorContext
{
    public long MessageId { get; set; }
    public int TelegramMessageId { get; set; }
}