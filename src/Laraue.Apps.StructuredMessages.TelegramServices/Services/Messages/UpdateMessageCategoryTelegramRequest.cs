using Laraue.Telegram.NET.Abstractions.Request;

namespace Laraue.Apps.StructuredMessages.TelegramServices.Services.Messages;

public class UpdateMessageCategoryTelegramRequest
{
    [FromQuery(ParameterNames.Id)]
    public required long Id { get; set; }
    
    [FromQuery(ParameterNames.CategoryId)]
    public required long CategoryId { get; set; }
}