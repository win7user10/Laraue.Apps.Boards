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
    
    Task<T> GetAvailable<T>(
        OrganizationAuthData authData,
        Filter filter,
        Func<IQueryable<EpicWithAccessLevel>, Task<T>> map,
        CancellationToken cancellationToken);
    
    Task HasAccessOrThrow(
        OrganizationAuthData authData,
        long epicId,
        ItemAccessLevel itemAccessLevel,
        CancellationToken cancellationToken);
}

public class Filter
{
    public long? SpaceId { get; set; }
}

public class EpicsAccessService(DatabaseContext context, IAccessService accessService) : IEpicsAccessService
{
    public Task<T> GetAvailable<T>(
        OrganizationAuthData authData,
        Func<IQueryable<EpicWithAccessLevel>, Task<T>> map, CancellationToken cancellationToken)
    {
        return GetAvailable(authData, new Filter(), map, cancellationToken);
    }

    public async Task<T> GetAvailable<T>(
        OrganizationAuthData authData,
        Filter filter,
        Func<IQueryable<EpicWithAccessLevel>, Task<T>> map,
        CancellationToken cancellationToken)
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
            return await map(GetGlobalReadableEpicsQuery(authData, globalAccess, filter));
        
        return await map(GetDirectReadableEpicsQuery(authData, filter));
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
    
    
    private IQueryable<EpicWithAccessLevel> GetGlobalReadableEpicsQuery(
        OrganizationAuthData authData,
        ItemAccessLevel accessLevel,
        Filter filter)
    {
        var query = context.Epics.AsQueryable();
        if (filter.SpaceId.HasValue)
            query = query.Where(e => e.SpaceId == filter.SpaceId.Value);
        
        return query
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
            .Select(epicData => new EpicWithAccessLevel
            {
                Epic = epicData.Epic,
                AccessLevel = epicData.SpaceOrganizationUser != null && epicData.EpicOrganizationUser != null
                    ? epicData.SpaceOrganizationUser.ItemAccessLevel | epicData.EpicOrganizationUser.ItemAccessLevel | accessLevel
                    : epicData.SpaceOrganizationUser != null
                        ? epicData.SpaceOrganizationUser.ItemAccessLevel | accessLevel
                        : epicData.EpicOrganizationUser != null
                            ? epicData.EpicOrganizationUser.ItemAccessLevel | accessLevel
                            : accessLevel
            });
    }
    
    private IQueryable<EpicWithAccessLevel> GetDirectReadableEpicsQuery(OrganizationAuthData authData, Filter filter)
    {
        var epicsQuery = context.Epics.AsQueryable();
        
        if (filter.SpaceId.HasValue)
            epicsQuery = epicsQuery.Where(e => e.Space!.Id == filter.SpaceId.Value);
        
        var all = epicsQuery
            .LeftJoin(
                context.Spaces,
                (e, s) => e.SpaceId == s.Id,
                (e, s) => new { Epic = e, Space = s })
            .LeftJoin(
                context.SpaceOrganizationUsers,
                (e, s) =>
                    e.Space.Id == s.SpaceId
                    && (s.ItemAccessLevel & ItemAccessLevel.ReadItems) == ItemAccessLevel.ReadItems
                    && s.OrganizationUser!.UserId == authData.UserId
                    && s.OrganizationUser!.OrganizationId == authData.OrganizationId,
                (e, s) => new { e.Epic, e.Space, SpaceOrganizationUser = s })
            .LeftJoin(
                context.EpicOrganizationUsers,
                (e, o) =>
                    e.Epic.Id == o.Id
                    && (o.ItemAccessLevel & ItemAccessLevel.ReadItems) == ItemAccessLevel.ReadItems
                    && o.OrganizationUser!.UserId == authData.UserId
                    && o.OrganizationUser!.OrganizationId == authData.OrganizationId,
                (e, o) => new { e.Epic, e.Space, e.SpaceOrganizationUser, EpicOrganizationUser = o })
            .Where(x => x.EpicOrganizationUser != null ||  x.SpaceOrganizationUser != null)
            .Select(e => new EpicWithAccessLevel
            {
                Epic = e.Epic,
                AccessLevel = e.SpaceOrganizationUser != null && e.EpicOrganizationUser != null
                    ? e.SpaceOrganizationUser.ItemAccessLevel | e.EpicOrganizationUser.ItemAccessLevel
                    : e.SpaceOrganizationUser != null 
                        ? e.SpaceOrganizationUser.ItemAccessLevel
                        : e.SpaceOrganizationUser!.ItemAccessLevel
            });
        
        return all;
    }
}

public record EpicWithAccessLevel
{
    public required Epic Epic { get; init; }
    public required ItemAccessLevel AccessLevel { get; init; }
}