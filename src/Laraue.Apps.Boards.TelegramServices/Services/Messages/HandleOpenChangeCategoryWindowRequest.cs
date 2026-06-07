using Laraue.Telegram.NET.Abstractions.Request;

namespace Laraue.Apps.Boards.TelegramServices.Services.Messages;

public class HandleOpenChangeCategoryWindowRequest
{
    [FromQuery(ParameterNames.MessageId)]
    public required long MessageId { get; set; }
}