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
    public Task<User?> FindAsync(long telegramId, CancellationToken cancellationToken = default)
    {
        return context.Users
            .Where(u => u.TelegramId == telegramId)
            .FirstOrDefaultAsyncEF(cancellationToken);
    }

    public async Task<Guid> CreateAsync(User user, CancellationToken cancellationToken = default)
    {
        var timestamp = dateTimeProvider.UtcNow;

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        
        user.Color = Palette.RandomColor();
        context.Users.Add(user);
        await context.SaveChangesAsync(cancellationToken);

        var organization = OrganizationDefaults.GetNewOrganizationEntity(
            user.Id,
            user.TelegramLanguageCode == "ru" ? "Без организации" : "No organization", // TODO - move to lang files
            Palette.RandomColor(),
            timestamp,
            isPersonal: true);
        
        context.Organizations.Add(organization);
        await context.SaveChangesAsync(cancellationToken);
        
        await transaction.CommitAsync(cancellationToken);
        
        return user.Id;
    }
}