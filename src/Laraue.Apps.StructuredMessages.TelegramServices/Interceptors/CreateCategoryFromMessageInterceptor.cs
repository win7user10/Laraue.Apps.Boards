using Laraue.Apps.StructuredMessages.Services;
using Laraue.Apps.StructuredMessages.TelegramServices.Services.Messages;
using Laraue.Telegram.NET.Authentication.Services;
using Laraue.Telegram.NET.Interceptors.Services;

namespace Laraue.Apps.StructuredMessages.TelegramServices.Interceptors;

public class CreateCategoryFromMessageInterceptor(
    TelegramRequestContext<Guid> requestContext,
    IInterceptorState<Guid> interceptorState,
    ICoreCategoryService coreCategoryService,
    ITelegramMessageService telegramMessageService,
    ICoreMessageService coreMessageService)
    : BaseRequestInterceptor<Guid, string, CreateCategoryFromMessageInterceptorContext>(
        requestContext,
        interceptorState)
{
    protected override Task ValidateAsync(
        TelegramRequestContext<Guid> requestContext,
        InterceptResult<string> interceptResult,
        CreateCategoryFromMessageInterceptorContext interceptorContext,
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
        CreateCategoryFromMessageInterceptorContext interceptorContext,
        CancellationToken cancellationToken = default)
    {
        var categoryId = await coreCategoryService.Create(
            new CreateMessageCategoryRequest
            {
                Name = model,
                UserId = requestContext.UserId,
            }, cancellationToken);

        await coreMessageService.UpdateCategory(
            interceptorContext.MessageId,
            categoryId,
            cancellationToken);

        await telegramMessageService
            .UpdateMessageInChat(
                interceptorContext.MessageId,
                editMessageId: null,
                cancellationToken);

        return ExecutionState.FullyExecuted;
    }

    public override string Id => "CreateCategory";
}

public class CreateCategoryFromMessageInterceptorContext
{
    public required long MessageId { get; set; }
}