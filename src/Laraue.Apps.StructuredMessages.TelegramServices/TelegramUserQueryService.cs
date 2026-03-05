using Laraue.Apps.StructuredMessages.DataAccess;
using Laraue.Apps.StructuredMessages.DataAccess.Models;
using Laraue.Telegram.NET.Authentication.Services;
using LinqToDB.EntityFrameworkCore;

namespace Laraue.Apps.StructuredMessages.TelegramServices;

public class TelegramUserQueryService(DatabaseContext context)
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
        context.Users.Add(user);
        
        await context.SaveChangesAsync();
        
        return user.Id;
    }
}