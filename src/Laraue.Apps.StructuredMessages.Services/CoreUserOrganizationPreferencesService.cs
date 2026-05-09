using Laraue.Apps.StructuredMessages.DataAccess;
using Laraue.Apps.StructuredMessages.DataAccess.Models;
using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace Laraue.Apps.StructuredMessages.Services;

public interface ICoreUserOrganizationPreferencesService
{
    Task Update(
        OrganizationAuthData authData,
        Action<UpdateSettersBuilder<UserOrganizationPreferences>> updateSetters,
        CancellationToken cancellationToken);
    
    Task<UserOrganizationPreferencesResponse> GetPreferences(
        OrganizationAuthData authData,
        CancellationToken cancellationToken);
}

public class CoreUserOrganizationPreferencesService(DatabaseContext context) : ICoreUserOrganizationPreferencesService
{
    public async Task Update(
        OrganizationAuthData authData,
        Action<UpdateSettersBuilder<UserOrganizationPreferences>> updateSetters,
        CancellationToken cancellationToken)
    {
        var updatedCount = await context.UserOrganizationPreferences
            .Where(x => x.UserId == authData.UserId)
            .Where(x => x.OrganizationId == authData.OrganizationId)
            .ExecuteUpdateAsync(updateSetters, cancellationToken);
        
        if (updatedCount > 0)
            return;
        
        // The first settings setup
        var preferences = GetDefaultPreferences(authData);
        context.Add(preferences);
        
        await context.SaveChangesAsync(cancellationToken);
        await context.UserOrganizationPreferences
            .Where(x => x.UserId == authData.UserId)
            .Where(x => x.OrganizationId == authData.OrganizationId)
            .ExecuteUpdateAsync(updateSetters, cancellationToken);
    }

    public async Task<UserOrganizationPreferencesResponse> GetPreferences(
        OrganizationAuthData authData,
        CancellationToken cancellationToken)
    {
        var preferences = await context.UserOrganizationPreferences
            .Where(x => x.UserId == authData.UserId)
            .Where(x => x.OrganizationId == authData.OrganizationId)
            .FirstOrDefaultAsyncEF(cancellationToken)
                ?? GetDefaultPreferences(authData);

        return new UserOrganizationPreferencesResponse
        {
            SelectedSpaceId = preferences.SelectedSpaceId,
        };
    }
    
    private static UserOrganizationPreferences GetDefaultPreferences(OrganizationAuthData authData)
    {
        return new UserOrganizationPreferences
        {
            UserId = authData.UserId,
            OrganizationId = authData.OrganizationId,
        };
    }
}

public record UserOrganizationPreferencesResponse
{
    public required long? SelectedSpaceId { get; init; }
}