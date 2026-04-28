using Laraue.Apps.StructuredMessages.DataAccess;
using Laraue.Apps.StructuredMessages.DataAccess.Enums;
using Laraue.Apps.StructuredMessages.DataAccess.Models;
using Laraue.Core.DataAccess.EFCore.Extensions;
using LinqToDB;
using Microsoft.EntityFrameworkCore;

namespace Laraue.Apps.StructuredMessages.Services;

public interface IEpicsAccessService
{
    /// <summary>
    /// Returns epics available for the user.
    /// </summary>
    /// <remarks>Use only with Linq2DB provider.</remarks>
    Task<T> GetAvailable<T>(
        OrganizationAuthData authData,
        Func<IQueryable<EpicWithAccessLevel>, Task<T>> map,
        CancellationToken cancellationToken);
    
    Task HasAccessOrThrow(
        OrganizationAuthData authData,
        long epicId,
        ItemAccessLevel itemAccessLevel,
        CancellationToken cancellationToken);
}

public class EpicsAccessService(DatabaseContext context, IAccessService accessService) : IEpicsAccessService
{
    public async Task<T> GetAvailable<T>(
        OrganizationAuthData authData,
        Func<IQueryable<EpicWithAccessLevel>, Task<T>> map, CancellationToken cancellationToken)
    {
        // TODO - all global permissions can live separately and requesting via one query.
        var globalOrganizationAccess = await accessService
            .GetGlobalOrganizationAccess(authData, cancellationToken);

        var globalSpaceAccess = await accessService
            .GetGlobalSpacesAccess(authData, cancellationToken);
        
        var globalEpicAccess = await accessService
            .GetGlobalEpicsAccess(authData, cancellationToken);
        
        var globalAccess = globalOrganizationAccess | globalSpaceAccess | globalEpicAccess;
        if (globalAccess.HasFlag(ItemAccessLevel.ReadItems))
            return await map(GetGlobalReadableEpicsQuery(authData, globalAccess));
        
        return await map(GetDirectReadableEpicsQuery(authData));
    }

    public async Task HasAccessOrThrow(
        OrganizationAuthData authData,
        long epicId,
        ItemAccessLevel itemAccessLevel,
        CancellationToken cancellationToken)
    {
        var globalOrganizationAccess = await accessService
            .GetGlobalOrganizationAccess(authData, cancellationToken);
        
        if (globalOrganizationAccess.HasFlag(itemAccessLevel))
            return;
        
        var globalSpaceAccess = await accessService
            .GetGlobalSpacesAccess(authData, cancellationToken);
        
        if (globalSpaceAccess.HasFlag(itemAccessLevel))
            return;
        
        var globalEpicAccess = await accessService
            .GetGlobalEpicsAccess(authData, cancellationToken);
        
        if (globalEpicAccess.HasFlag(itemAccessLevel))
            return;

        var epic = await context.Epics
            .Select(x => new { x.SpaceId })
            .FirstOrThrowNotFoundEFAsync($"Epic: {epicId} is not found", cancellationToken);
        
        var hasDirectAccessFromSpace = await context.SpaceOrganizationUsers
            .Where(sos => sos.OrganizationUser!.OrganizationId == authData.OrganizationId)
            .Where(sos => sos.OrganizationUser!.UserId == authData.UserId)
            .Where(sos => sos.SpaceId == epic.SpaceId)
            .AnyAsync(sos => sos.ItemAccessLevel.HasFlag(itemAccessLevel), cancellationToken);
        
        if (hasDirectAccessFromSpace)
            return;
        
        await context.EpicOrganizationUsers
            .Where(eou => eou.OrganizationUser!.OrganizationId == authData.OrganizationId)
            .Where(eou => eou.OrganizationUser!.UserId == authData.UserId)
            .Where(eou => eou.EpicId == epicId)
            .AnyOrThrowNotFoundEFAsync(
                eou => eou.ItemAccessLevel.HasFlag(itemAccessLevel),
                $"Space: {epic} is unavailable or permission: {itemAccessLevel} is missing",
                cancellationToken);
    }
    
    
    private IQueryable<EpicWithAccessLevel> GetGlobalReadableEpicsQuery(OrganizationAuthData authData, ItemAccessLevel accessLevel)
    {
        return context.Epics
            .Where(s => s.Space!.OrganizationId == authData.OrganizationId)
            .LeftJoin(
                context.EpicOrganizationUsers,
                (epic, user) => epic.Id == user.EpicId,
                (epic, user) => new { Epic = epic, EpicOrganizationUser = (EpicOrganizationUser?)user })
            .LeftJoin(
                context.SpaceOrganizationUsers,
                (join, user) => join.Epic.SpaceId == user.SpaceId,
                (join, user) => new
                {
                    join.Epic,
                    join.EpicOrganizationUser,
                    SpaceOrganizationUser = (SpaceOrganizationUser?)user
                })
            .Select(epicData => new EpicWithAccessLevel(
                epicData.Epic,
                epicData.SpaceOrganizationUser != null && epicData.EpicOrganizationUser != null
                    ? epicData.SpaceOrganizationUser.ItemAccessLevel | epicData.EpicOrganizationUser.ItemAccessLevel | accessLevel
                    : epicData.SpaceOrganizationUser != null
                        ? epicData.SpaceOrganizationUser.ItemAccessLevel | accessLevel
                        : epicData.EpicOrganizationUser != null
                            ? epicData.EpicOrganizationUser.ItemAccessLevel | accessLevel
                            : accessLevel));
    }
    
    private IQueryable<EpicWithAccessLevel> GetDirectReadableEpicsQuery(OrganizationAuthData authData)
    {
        var epicsFromSpaces = context.SpaceOrganizationUsers
            .Where(sos => sos.OrganizationUser!.OrganizationId == authData.OrganizationId)
            .Where(sos => sos.OrganizationUser!.UserId == authData.UserId)
            .Where(sos => sos.ItemAccessLevel.HasFlag(ItemAccessLevel.ReadItems))
            .SelectMany(sos => sos.Space!.Epics!.Select(e => new
            {
                EpicId = e.Id,
                Epic = e,
                AccessLevel = sos.ItemAccessLevel
            }));

        var all = epicsFromSpaces
            .FullJoin(
                context.EpicOrganizationUsers,
                (level, user) =>
                    level.EpicId == user.EpicId
                    && user.OrganizationUser!.UserId == authData.UserId
                    && user.OrganizationUser!.OrganizationId == authData.OrganizationId
                    && user.ItemAccessLevel.HasFlag(ItemAccessLevel.ReadItems),
                (level, user) => new EpicWithAccessLevel(
                    level != null ? level.Epic : user.Epic,
                    level != null && user != null
                        ? level.AccessLevel | user.ItemAccessLevel
                        : level != null 
                            ? level.AccessLevel
                            : user.ItemAccessLevel));
        
        return all;
    }
}

public record EpicWithAccessLevel(Epic Epic, ItemAccessLevel AccessLevel);