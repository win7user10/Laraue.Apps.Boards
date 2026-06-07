using Microsoft.AspNetCore.Mvc;

namespace Laraue.Apps.Boards.WebApiHost.Controllers;

[ApiController]
[Route("/api/user")]
public class TelegramAuthController(ITelegramAuthService authService)
    : ControllerBase
{
    [HttpPost("auth-via-mini-app")]
    public Task<string> Authenticate(
        AuthenticateViaStringInitDataRequest request,
        CancellationToken cancellationToken)
    {
        return authService.Authenticate(request, cancellationToken);
    }
    
    [HttpPost("auth")]
    public Task<string> Authenticate(
        TelegramWidgetAuthRequest request,
        CancellationToken cancellationToken)
    {
        return authService.Authenticate(request, cancellationToken);
    }
}