using Laraue.Apps.StructuredMessages.DataAccess.Enums;
using Laraue.Apps.StructuredMessages.Services;

namespace Laraue.Apps.StructuredMessages.WebApiServices;

public interface IUserOrganizationPreferencesService
{
    Task UpdateSelectedSpace(
        OrganizationAuthData authData,
        long spaceId,
        CancellationToken cancellationToken = default);
    
    Task<UserOrganizationPreferencesResponse> GetPreferences(
        OrganizationAuthData authData,
        CancellationToken cancellationToken);
}

public class UserOrganizationPreferencesService(
    ICoreUserOrganizationPreferencesService coreService,
    ISpacesAccessService spacesAccessService)
    : IUserOrganizationPreferencesService
{
    public async Task UpdateSelectedSpace(
        OrganizationAuthData authData,
        long spaceId,
        CancellationToken cancellationToken = default)
    {
        await spacesAccessService.HasAccessOrThrow(
            authData,
            spaceId,
            EntityAccessLevel.Read,
            cancellationToken);
        
        await coreService
            .Update(
                authData,
                update => update.SetProperty(p => p.SelectedSpaceId, spaceId),
                cancellationToken);
    }

    public Task<UserOrganizationPreferencesResponse> GetPreferences(
        OrganizationAuthData authData
        , CancellationToken cancellationToken)
    {
        return coreService.GetPreferences(authData, cancellationToken);
    }
}