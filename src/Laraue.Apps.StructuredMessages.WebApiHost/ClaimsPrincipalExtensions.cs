using System.Security.Claims;

namespace Laraue.Apps.StructuredMessages.WebApiHost;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetId(this ClaimsPrincipal principal)
    {
        var idClaim = principal.Claims.FirstOrDefault(x => x.Type == "id")
            ?? throw new InvalidOperationException("User does not contain ID claim");
        
        return Guid.Parse(idClaim.Value);
    }
}