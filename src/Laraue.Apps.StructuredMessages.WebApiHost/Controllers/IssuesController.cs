using Laraue.Apps.StructuredMessages.WebApiServices;
using Laraue.Core.DataAccess.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Laraue.Apps.StructuredMessages.WebApiHost.Controllers;

[Authorize(AuthenticationSchemes = AuthSchemas.Organization)]
[ApiController]
[Route("/api/issues")]
public class IssuesController(IIssuesService issuesService) : ControllerBase
{
    [HttpGet("by-status/{statusId:long}")]
    public Task<BatchResult<IssueListDto>> GetIssuesByStatus(
        long statusId,
        [FromQuery] GetIssuesRequest request,
        CancellationToken cancellationToken = default)
    {
        return issuesService.GetIssues(
            request with
            {
                StatusId = statusId,
                AuthData = HttpContext.User.GetOrganizationAuthData(),
            },
            cancellationToken);
    }
    
    
    [HttpGet("{id:long}")]
    public Task<IssueDetailDto> GetIssue(
        long id,
        CancellationToken cancellationToken = default)
    {
        return issuesService.GetIssue(
            new GetIssueRequest
            {
                UserId = HttpContext.User.GetId(),
                IssueId = id,
            },
            cancellationToken);
    }
    
    [HttpGet("board")]
    public Task<ColumnMessages[]> GetBoard(
        [FromQuery] GetBoardRequest request,
        CancellationToken cancellationToken = default)
    {
        return issuesService.GetBoard(
            request with
            {
                AuthData = HttpContext.User.GetOrganizationAuthData(),
            },
            cancellationToken);
    }
    
    [HttpPut("{id:long}/move")]
    public Task Move(
        long id,
        [FromBody] MoveIssueRequest request,
        CancellationToken cancellationToken = default)
    {
        return issuesService.Move(
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
        return issuesService.DeleteMessage(
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
        return issuesService.Create(
            request with
            {
                AuthData = HttpContext.User.GetOrganizationAuthData(),
            },
            cancellationToken);
    }
    
    [HttpPut("{id:long}")]
    public Task Update(
        [FromRoute] long id,
        [FromBody] UpdateIssueRequest request,
        CancellationToken cancellationToken = default)
    {
        return issuesService.UpdateIssue(
            request with
            {
                UserId = HttpContext.User.GetId(),
                Id = id,
            },
            cancellationToken);
    }
    
    [HttpGet("search")]
    public Task<ShortPaginatedResult<IssueListDto>> Search(
        [FromQuery] SearchRequest request,
        CancellationToken cancellationToken = default)
    {
        return issuesService.Search(
            request with
            {
                UserId = HttpContext.User.GetId(),
            },
            cancellationToken);
    }

    /*[HttpGet("summary")] Need to rethink the concept of summary
    public Task<CategorySummary[]> GetBoardSummary(
        [FromQuery] GetBoardSummaryRequest request,
        CancellationToken cancellationToken = default)
    {
        return issuesService.GetBoardSummary(
            request with
            {
                UserId = HttpContext.User.GetId(),
            },
            cancellationToken);
    }*/
}   