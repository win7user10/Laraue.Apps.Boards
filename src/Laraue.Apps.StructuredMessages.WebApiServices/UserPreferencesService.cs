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
        if (await spacesService.UserHasAccessToSpace(
            userId,
            spaceId,
            AccessType.Read,
            cancellationToken))
        {
            await coreService
                .Update(
                    userId,
                    update => update.SetProperty(p => p.SpaceId, spaceId),
                    cancellationToken);
        }
    }

    public Task<UserPreferencesResponse> GetPreferences(Guid userId, CancellationToken cancellationToken)
    {
        return coreService.GetPreferences(userId, cancellationToken);
    }
}