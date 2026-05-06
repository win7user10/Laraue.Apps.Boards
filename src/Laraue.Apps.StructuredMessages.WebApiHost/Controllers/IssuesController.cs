using Laraue.Apps.StructuredMessages.WebApiServices;
using Laraue.Core.DataAccess.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Laraue.Apps.StructuredMessages.WebApiHost.Controllers;

[Authorize(AuthenticationSchemes = AuthSchemas.Organization)]
[ApiController]
[Route("/api/issues")]
public class IssuesController(IIssuesService messagesService) : ControllerBase
{
    [HttpGet]
    public Task<BatchResult<MessageListDto>> GetMessages(
        [FromQuery] GetMessagesRequest request,
        CancellationToken cancellationToken = default)
    {
        return messagesService.GetMessages(
            request with
            {
                AuthData = HttpContext.User.GetOrganizationAuthData(),
            },
            cancellationToken);
    }
    
    
    [HttpGet("{id:long}")]
    public Task<MessageDetailDto> GetMessage(
        long id,
        CancellationToken cancellationToken = default)
    {
        return messagesService.GetMessage(
            new GetMessageRequest
            {
                UserId = HttpContext.User.GetId(),
                MessageId = id,
            },
            cancellationToken);
    }
    
    [HttpGet("board")]
    public Task<ColumnMessages[]> GetBoard(
        [FromQuery] GetBoardRequest request,
        CancellationToken cancellationToken = default)
    {
        return messagesService.GetBoard(
            request with
            {
                AuthData = HttpContext.User.GetOrganizationAuthData(),
            },
            cancellationToken);
    }
    
    [HttpPut("{id:long}/move")]
    public Task Move(
        long id,
        MoveCardRequest request,
        CancellationToken cancellationToken = default)
    {
        return messagesService.Move(
            request with
            {
                UserId = HttpContext.User.GetId(),
                IssueId = id
            },
            cancellationToken);
    }
    
    [HttpDelete("{id:long}")]
    public Task Delete(
        long id,
        CancellationToken cancellationToken = default)
    {
        return messagesService.DeleteMessage(
            new DeleteMessageRequest
            {
                UserId = HttpContext.User.GetId(),
                MessageId = id,
            },
            cancellationToken);
    }
    
    [HttpPost]
    public Task<long> Create(
        [FromBody] CreateIssueRequest request,
        CancellationToken cancellationToken = default)
    {
        return messagesService.Create(
            request with
            {
                AuthData = HttpContext.User.GetOrganizationAuthData(),
            },
            cancellationToken);
    }
    
    [HttpPut("{id:long}")]
    public Task Update(
        [FromRoute] long id,
        [FromBody] EditMessageRequest request,
        CancellationToken cancellationToken = default)
    {
        return messagesService.EditMessage(
            request with
            {
                UserId = HttpContext.User.GetId(),
                MessageId = id,
            },
            cancellationToken);
    }
    
    [HttpGet("search")]
    public Task<IShortPaginatedResult<MessageListDto>> Search(
        [FromQuery] SearchRequest request,
        CancellationToken cancellationToken = default)
    {
        return messagesService.Search(
            request with
            {
                UserId = HttpContext.User.GetId(),
            },
            cancellationToken);
    }

    [HttpGet("summary")]
    public Task<CategorySummary[]> GetBoardSummary(
        [FromQuery] GetBoardSummaryRequest request,
        CancellationToken cancellationToken = default)
    {
        return messagesService.GetBoardSummary(
            request with
            {
                UserId = HttpContext.User.GetId(),
            },
            cancellationToken);
    }
}   