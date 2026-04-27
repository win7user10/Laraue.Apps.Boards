using Laraue.Apps.StructuredMessages.DataAccess.Enums;
using Laraue.Apps.StructuredMessages.DataAccess.Models;
using Laraue.Apps.StructuredMessages.Services;

namespace Laraue.Apps.StructuredMessages.WebApiServices;

public interface IUserPreferencesService
{
    Task UpdateEpicSortOrder(
        Guid userId,
        EpicSortOrder epicSortOrder,
        CancellationToken cancellationToken = default);
    
    Task UpdateSpace(
        Guid userId,
        long spaceId,
        CancellationToken cancellationToken = default);
    
    Task<UserPreferencesResponse> GetPreferences(
        Guid userId,
        CancellationToken cancellationToken);
}

public class UserPreferencesService(ICoreUserPreferencesService coreService, ICoreSpacesService spacesService) : IUserPreferencesService
{
    public Task UpdateEpicSortOrder(
        Guid userId,
        EpicSortOrder epicSortOrder,
        CancellationToken cancellationToken = default)
    {
        return coreService
            .Update(
                userId,
                update => update.SetProperty(p => p.EpicSortOrder, epicSortOrder),
                cancellationToken);
    }

    public async Task UpdateSpace(Guid userId, long spaceId, CancellationToken cancellationToken = default)
    {
        var value = IdService.ToNullableId(spaceId);
        if (value.HasValue && !await spacesService.UserHasAccessToSpace(
            userId,
            spaceId,
            ItemsAccessLevel.Read,
            cancellationToken))
            return;
        
        await coreService
            .Update(
                userId,
                update => update.SetProperty(p => p.SpaceId, value),
                cancellationToken);
    }

    public Task<UserPreferencesResponse> GetPreferences(Guid userId, CancellationToken cancellationToken)
    {
        return coreService.GetPreferences(userId, cancellationToken);
    }
}