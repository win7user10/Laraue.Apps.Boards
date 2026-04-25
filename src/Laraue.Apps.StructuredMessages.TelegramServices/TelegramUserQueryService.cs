using Laraue.Apps.StructuredMessages.DataAccess;
using Laraue.Apps.StructuredMessages.DataAccess.Models;
using Laraue.Apps.StructuredMessages.Services;
using Laraue.Core.DateTime.Services.Abstractions;
using Laraue.Telegram.NET.Authentication.Services;
using LinqToDB.EntityFrameworkCore;

namespace Laraue.Apps.StructuredMessages.TelegramServices;

public class TelegramUserQueryService(DatabaseContext context, IDateTimeProvider dateTimeProvider)
    : ITelegramUserQueryService<User, Guid>
{
    public Task<User?> FindAsync(long telegramId)
    {
        return context.Users
            .Where(u => u.TelegramId == telegramId)
            .FirstOrDefaultAsyncEF();
    }

    public async Task<Guid> CreateAsync(User user)
    {
        var timestamp = dateTimeProvider.UtcNow;
        
        user.Color = Palette.RandomColor();
        user.Organizations = new List<Organization>
        {
            OrganizationDefaults.GetNewOrganizationEntity(
                user.Id,
                "Personal",
                Palette.RandomColor(),
                timestamp,
                OrganizationType.Personal)
        };
        
        context.Users.Add(user);
        await context.SaveChangesAsync();
        
        return user.Id;
    }
}