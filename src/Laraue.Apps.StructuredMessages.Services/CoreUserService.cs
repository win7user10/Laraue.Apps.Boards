using Laraue.Apps.StructuredMessages.DataAccess;
using Laraue.Apps.StructuredMessages.DataAccess.Models;
using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace Laraue.Apps.StructuredMessages.Services;

public interface ICoreUserService
{
    Task UpdatePreferences(
        Guid userId,
        Action<UpdateSettersBuilder<UserPreferences>> updateSetters,
        CancellationToken cancellationToken);
    
    Task<UserPreferencesResponse> GetPreferences(
        Guid userId,
        CancellationToken cancellationToken);
}

public class CoreUserService(DatabaseContext context) : ICoreUserService
{
    public async Task UpdatePreferences(
        Guid userId,
        Action<UpdateSettersBuilder<UserPreferences>> updateSetters,
        CancellationToken cancellationToken)
    {
        var updatedCount = await context.UserPreferences
            .Where(x => x.UserId == userId)
            .ExecuteUpdateAsync(updateSetters, cancellationToken);
        
        if (updatedCount > 0)
            return;
        
        // The first settings setup
        var preferences = GetDefaultPreferences(userId);
        context.Add(preferences);
        
        await context.SaveChangesAsync(cancellationToken);
        await context.UserPreferences
            .Where(x => x.UserId == userId)
            .ExecuteUpdateAsync(updateSetters, cancellationToken);
    }

    public async Task<UserPreferencesResponse> GetPreferences(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var preferences = await context.UserPreferences
            .Where(x => x.UserId == userId)
            .FirstOrDefaultAsyncEF(cancellationToken)
            ?? GetDefaultPreferences(userId);

        return new UserPreferencesResponse
        {
            EpicSortOrder = preferences.EpicSortOrder,
        };
    }
    
    private static UserPreferences GetDefaultPreferences(Guid userId)
    {
        return new UserPreferences
        {
            UserId = userId,
            EpicSortOrder = EpicSortOrder.LastTouched
        };
    }
}

public record UserPreferencesResponse
{
    public EpicSortOrder EpicSortOrder { get; init; }
}