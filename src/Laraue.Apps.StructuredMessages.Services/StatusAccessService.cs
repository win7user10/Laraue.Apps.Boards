using Laraue.Apps.StructuredMessages.DataAccess;
using Laraue.Apps.StructuredMessages.DataAccess.Enums;
using Laraue.Core.DataAccess.EFCore.Extensions;
using Laraue.Core.Exceptions.Web;

namespace Laraue.Apps.StructuredMessages.Services;

public interface IStatusAccessService
{
    Task CanMoveToStatusOrThrow(
        OrganizationAuthData authData,
        long statusId,
        CancellationToken cancellationToken);
    
    Task CanModifyStatusOrThrow(
        OrganizationAuthData authData,
        long statusId,
        CancellationToken cancellationToken);
}

public class StatusAccessService(
    IEpicsAccessService epicsAccessService,
    DatabaseContext context)
    : IStatusAccessService
{
    public async Task CanMoveToStatusOrThrow(
        OrganizationAuthData authData,
        long statusId,
        CancellationToken cancellationToken)
    {
        var epicId = await GetEpicId(statusId, cancellationToken);

        try
        {
            await epicsAccessService.HasAccessOrThrow(
                authData,
                epicId,
                ChildrenAccessLevel.Create,
                cancellationToken);
        }
        catch (NotFoundException)
        {
            throw new NotFoundException(GetError(statusId));
        }
    }

    public async Task CanModifyStatusOrThrow(OrganizationAuthData authData, long statusId, CancellationToken cancellationToken)
    {
        var epicId = await GetEpicId(statusId, cancellationToken);
        
        try
        {
            await epicsAccessService.HasAccessOrThrow(
                authData,
                epicId,
                EntityAccessLevel.Update,
                cancellationToken);
        }
        catch (NotFoundException)
        {
            throw new NotFoundException(GetError(statusId));
        }
    }

    private Task<long> GetEpicId(long statusId, CancellationToken cancellationToken)
    {
        return context.Statuses
            .Where(s => s.Id == statusId)
            .Select(s => s.EpicId)
            .FirstOrThrowNotFoundEFAsync(GetError(statusId), cancellationToken);
    }

    private static string GetError(long statusId)
    {
        return $"Status: {statusId} is not exists or permission: {ChildrenAccessLevel.Update} missing on Epic";
    }
}