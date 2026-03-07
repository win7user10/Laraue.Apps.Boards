using Laraue.Telegram.NET.Abstractions.Request;

namespace Laraue.Apps.StructuredMessages.TelegramServices.Services.Messages;

public class CreateCategoryFromMessageTelegramRequest
{
    [FromQuery(ParameterNames.TelegramMessageId)]
    public required int MessageId { get; set; }
}