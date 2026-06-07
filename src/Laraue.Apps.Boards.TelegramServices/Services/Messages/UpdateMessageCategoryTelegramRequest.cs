using Laraue.Telegram.NET.Abstractions.Request;

namespace Laraue.Apps.Boards.TelegramServices.Services.Messages;

public class UpdateMessageCategoryTelegramRequest
{
    [FromQuery(ParameterNames.MessageId)]
    public required long MessageId { get; set; }
    
    [FromQuery(ParameterNames.MessageCategoryId)]
    public required long CategoryId { get; set; }
}