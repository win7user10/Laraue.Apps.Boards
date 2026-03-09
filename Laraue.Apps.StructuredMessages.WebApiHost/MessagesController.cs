using Laraue.Apps.StructuredMessages.WebApiServices;
using Microsoft.AspNetCore.Mvc;

namespace Laraue.Apps.StructuredMessages.WebApiHost;

[ApiController]
[Route("/api/messages")]
public class MessagesController(IMessagesService messagesService) : ControllerBase
{
    [HttpGet]
    public Task<MessageListDto[]> GetMessages(CancellationToken cancellationToken)
        => messagesService.GetMessages(
            new GetMessagesRequest
            {
                UserId = new Guid("019cc73f-4bb9-79c0-bb7e-f35d6d911df4")
            },
            cancellationToken);
}   