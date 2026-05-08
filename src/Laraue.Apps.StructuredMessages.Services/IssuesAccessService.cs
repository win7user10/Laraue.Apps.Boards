using Laraue.Apps.StructuredMessages.DataAccess;
using Laraue.Apps.StructuredMessages.DataAccess.Enums;
using Laraue.Apps.StructuredMessages.DataAccess.Models;
using Laraue.Core.DataAccess.EFCore.Extensions;
using Laraue.Core.Exceptions.Web;

namespace Laraue.Apps.StructuredMessages.Services;

public interface IIssuesAccessService
{
    Task<T> GetAvailable<T>(
        OrganizationAuthData authData,
        Func<IQueryable<Issue>, Task<T>> map,
        CancellationToken cancellationToken);
    
    Task HasAccessOrThrow(
        OrganizationAuthData authData,
        long issueId,
        EntityAccessLevel entityAccessLevel,
        CancellationToken cancellationToken);
}

public class IssuesAccessService(IEpicsAccessService epicsAccessService, DatabaseContext context) : IIssuesAccessService
{
    public Task<T> GetAvailable<T>(
        OrganizationAuthData authData,
        Func<IQueryable<Issue>, Task<T>> map,
        CancellationToken cancellationToken)
    {
        return epicsAccessService.GetAvailable(
            authData,
            epics => map(epics.SelectMany(e => e.Epic.Statuses!.SelectMany(i => i.Issues!))),
            cancellationToken);
    }

    public async Task HasAccessOrThrow(
        OrganizationAuthData authData,
        long issueId,
        EntityAccessLevel entityAccessLevel,
        CancellationToken cancellationToken)
    {
        try
        {
            var epicId = await context.Issues
                .Where(i => i.Id == issueId)
                .Select(x => x.Status!.EpicId)
                .FirstOrThrowNotFoundEFAsync(string.Empty, cancellationToken);

            await epicsAccessService.HasAccessOrThrow(
                authData,
                epicId,
                entityAccessLevel.ToEntityAccessLevel(),
                cancellationToken);
        }
        catch (NotFoundException)
        {
            throw new NotFoundException($"Issue is not exists or epic children permission: {entityAccessLevel} is missing");
        }
    }
}