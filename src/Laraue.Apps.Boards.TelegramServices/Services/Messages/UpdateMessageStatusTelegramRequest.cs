using Laraue.Telegram.NET.Abstractions.Request;

namespace Laraue.Apps.Boards.TelegramServices.Services.Messages;

public class UpdateMessageStatusTelegramRequest
{
    [FromQuery(ParameterNames.MessageId)]
    public required long MessageId { get; set; }
    
    [FromQuery(ParameterNames.StatusId)]
    public required long StatusId { get; set; }
}