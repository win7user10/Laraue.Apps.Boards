using Laraue.Apps.StructuredMessages.DataAccess;
using Laraue.Core.DataAccess.EFCore.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Laraue.Apps.StructuredMessages.WebApiHost.Controllers;

[Authorize]
[ApiController]
[Route("/api/user")]
public class UserController(DatabaseContext context) : ControllerBase
{
    [HttpGet]
    public Task<UserDto> GetAsync(CancellationToken ct)
    {
        return context.Users
            .Where(x => x.Id == HttpContext.User.GetId())
            .Select(x => new UserDto
            {
                Username = x.TelegramUserName,
            })
            .FirstOrThrowNotFoundEFAsync(ct);
    }
}

public class UserDto
{
    public string? Username { get; set; }
}