using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;
using Laraue.Apps.StructuredMessages.DataAccess;
using Laraue.Core.Exceptions.Web;
using LinqToDB.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Laraue.Apps.StructuredMessages.WebApiHost.Controllers;

[ApiController]
[Route("/api/user")]
public class TelegramAuthController(
    IOptions<TelegramOptions> options,
    DatabaseContext context,
    IAuthService authService)
    : ControllerBase
{
    [HttpPost("validate")]
    public async Task<string> ValidateToken(
        ValidateRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryValidateUserId(request.InitData, out var telegramUserId))
        {
            throw new ForbiddenException("Authorization Failed");
        }

        var data = await context.Users
            .Where(x => x.TelegramId == telegramUserId)
            .Select(x => new
            {
                x.Id
            })
            .FirstAsyncEF(cancellationToken);

        return authService.CreateToken(data.Id);
    }

    public class ValidateRequest
    {
        public required string InitData { get; set; }
    }

    public bool TryValidateUserId(
        string initData,
        [NotNullWhen(true)] out long? telegramUserId)
    {
        telegramUserId = null;
        
        var parsedData = HttpUtility.ParseQueryString(initData);
        var receivedHash = parsedData["hash"];
        parsedData.Remove("hash");
        
        if (string.IsNullOrEmpty(receivedHash))
            return false;
        
        var sortedKeys = parsedData.AllKeys.OrderBy(key => key, StringComparer.Ordinal).ToList();
        var dataCheckStrings = sortedKeys.Select(key => $"{key}={parsedData[key]}");
        var dataCheckString = string.Join("\n", dataCheckStrings);

        var secretKey = HMACSHA256.HashData(
            "WebAppData"u8.ToArray(),
            Encoding.UTF8.GetBytes(options.Value.Token));
        
        var generatedHashBytes = HMACSHA256.HashData(
            secretKey,
            Encoding.UTF8.GetBytes(dataCheckString));
        
        var generatedHash = Convert.ToHexString(generatedHashBytes).ToLower();
        var result = generatedHash.Equals(receivedHash, StringComparison.OrdinalIgnoreCase);
        if (result)
        {
            var user = parsedData["user"];
            var userParsed = JsonSerializer.Deserialize<WebAppUser>(user!, JsonSerializerOptions.Web);
            telegramUserId = userParsed!.Id;
        }
        
        return result;
    }
}

public class WebAppUser
{
    public long Id { get; set; }
}

/*
export interface WebAppUser {
    id: number;
    is_bot?: boolean;
    first_name: string;
    last_name?: string;
    username?: string;
    language_code?: string;
    is_premium?: boolean;
    photo_url?: string;
}
*/