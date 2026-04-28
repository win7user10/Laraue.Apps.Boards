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
        OrganizationAuthData authData,
        long spaceId,
        CancellationToken cancellationToken = default);
    
    Task<UserPreferencesResponse> GetPreferences(
        Guid userId,
        CancellationToken cancellationToken);
}

public class UserPreferencesService(
    ICoreUserPreferencesService coreService,
    ISpacesAccessService spacesAccessService) : IUserPreferencesService
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

    public async Task UpdateSpace(OrganizationAuthData authData, long spaceId, CancellationToken cancellationToken = default)
    {
        await spacesAccessService.HasAccessOrThrow(
            authData,
            spaceId,
            ItemAccessLevel.ReadItems,
            cancellationToken);
        
        await coreService
            .Update(
                authData.UserId,
                update => update.SetProperty(p => p.SpaceId, spaceId),
                cancellationToken);
    }

    public Task<UserPreferencesResponse> GetPreferences(Guid userId, CancellationToken cancellationToken)
    {
        return coreService.GetPreferences(userId, cancellationToken);
    }
}