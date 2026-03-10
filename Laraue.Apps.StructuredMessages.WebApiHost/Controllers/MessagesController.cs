using Laraue.Apps.StructuredMessages.WebApiServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Laraue.Apps.StructuredMessages.WebApiHost.Controllers;

[Authorize]
[ApiController]
[Route("/api/messages")]
public class MessagesController(IMessagesService messagesService) : ControllerBase
{
    [HttpGet]
    public Task<MessageListDto[]> GetMessages(
        [FromQuery] GetMessagesRequest request,
        CancellationToken cancellationToken) =>
            messagesService.GetMessages(request with
            {
                UserId = HttpContext.User.GetId(),
            },
            cancellationToken);
    
    
    [HttpGet("backlog")]
    public Task<MessageListDto[]> GetBacklogMessages(CancellationToken cancellationToken) =>
        messagesService.GetBacklogMessages(new GetBacklogMessagesRequest
        {
            UserId = HttpContext.User.GetId(),
        },
        cancellationToken);
}   