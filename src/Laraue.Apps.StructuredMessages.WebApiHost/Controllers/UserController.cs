using Laraue.Apps.StructuredMessages.DataAccess;
using Laraue.Apps.StructuredMessages.Services;
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
    public async Task<UserDto> GetAsync(CancellationToken ct)
    {
        var user = await context.Users
            .Where(x => x.Id == HttpContext.User.GetId())
            .Select(x => new UserDto
            {
                Username = x.TelegramUserName,
                LanguageCode = InterfaceLanguage.ForCode(x.TelegramLanguageCode).Code,
                Color = "#3fb950",
                FirstName = x.TelegramFirstName,
                LastName = x.TelegramLastName,
                TelegramId = x.TelegramId,
            })
            .FirstOrThrowNotFoundEFAsync(ct);

        var initials = UserInitialsUtility.GetInitials(
            user.Username,
            user.FirstName,
            user.LastName,
            user.TelegramId);

        user.Initials = initials.Initial;
        return user;
    }
}

public class UserDto
{
    public long TelegramId { get; set; }
    public string? Username { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public required string LanguageCode { get; set; }
    public required string Color { get; set; }
    public string? Initials { get; set; }
}