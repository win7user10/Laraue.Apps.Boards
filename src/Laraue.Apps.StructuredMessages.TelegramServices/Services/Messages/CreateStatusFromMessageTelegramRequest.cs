using Laraue.Telegram.NET.Abstractions.Request;

namespace Laraue.Apps.StructuredMessages.TelegramServices.Services.Messages;

public class CreateStatusFromMessageTelegramRequest
{
    [FromQuery(ParameterNames.MessageId)]
    public required long MessageId { get; set; }
    
    [FromQuery(ParameterNames.MessageCategoryId)]
    public required long MessageCategoryId { get; set; }
}