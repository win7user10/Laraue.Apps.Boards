using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;
using Laraue.Apps.StructuredMessages.DataAccess;
using Laraue.Apps.StructuredMessages.DataAccess.Models;
using Laraue.Core.DateTime.Services.Abstractions;
using Laraue.Core.Exceptions.Web;
using LinqToDB.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Telegram.Bot;

namespace Laraue.Apps.StructuredMessages.WebApiHost.Controllers;

[ApiController]
[Route("/api/user")]
public class TelegramAuthController(
    IOptions<TelegramOptions> options,
    DatabaseContext context,
    IAuthService authService,
    IDateTimeProvider dateTimeProvider)
    : ControllerBase
{
    [HttpPost("validate")]
    public async Task<string> ValidateToken(
        ValidateRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryValidateUserId(request.InitData, out var userData))
        {
            throw new ForbiddenException("Authorization Failed");
        }

        var data = await context.Users
            .Where(x => x.TelegramId == userData.Id)
            .Select(x => new
            {
                x.Id
            })
            .FirstOrDefaultAsyncEF(cancellationToken);

        if (data is not null)
            return authService.CreateToken(data.Id);

        var newUserId = await RegisterUserFromWebApp(userData, cancellationToken);
        return authService.CreateToken(newUserId);
    }

    private async Task<Guid> RegisterUserFromWebApp(
        WebAppUser user,
        CancellationToken cancellationToken)
    {
        var newUser = new User
        {
            CreatedAt = dateTimeProvider.UtcNow,
            TelegramId = user.Id,
            TelegramLanguageCode = user.LanguageCode,
            TelegramUserName = user.Username,
            TelegramFirstName = user.FirstName,
            TelegramLastName = user.LastName,
        };
        
        context.Users.Add(newUser);
        await context.SaveChangesAsync(cancellationToken);

        return newUser.Id;
    }

    public class ValidateRequest
    {
        public required string InitData { get; set; }
    }

    public bool TryValidateUserId(
        string initData,
        [NotNullWhen(true)] out WebAppUser? webAppUser)
    {
        webAppUser = null;
        
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
            webAppUser = JsonSerializer.Deserialize<WebAppUser>(user!, JsonBotAPI.Options);
        }
        
        return result;
    }
}

public class WebAppUser
{
    public long Id { get; set; }
    public string? Username { get; set; }
    public required string LanguageCode { get; set; }
    public required string? FirstName { get; set; }
    public required string? LastName { get; set; }
}