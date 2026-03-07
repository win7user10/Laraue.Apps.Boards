using Laraue.Telegram.NET.Abstractions.Request;

namespace Laraue.Apps.StructuredMessages.TelegramServices.Services.Messages;

public class HandleChangeContentTelegramRequest
{
    [FromQuery(ParameterNames.MessageId)]
    public required long MessageId { get; set; }
}