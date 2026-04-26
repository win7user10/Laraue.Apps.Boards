using Laraue.Apps.StructuredMessages.DataAccess.Models;

namespace Laraue.Apps.StructuredMessages.Services;

public interface IIssuesAccessService
{
    Task<T> GetAvailable<T>(
        OrganizationAuthData authData,
        Func<IQueryable<Issue>, Task<T>> map,
        CancellationToken cancellationToken);
}


public class IssuesAccessService(IEpicsAccessService epicsAccessService) : IIssuesAccessService
{
    public Task<T> GetAvailable<T>(
        OrganizationAuthData authData,
        Func<IQueryable<Issue>, Task<T>> map,
        CancellationToken cancellationToken)
    {
        return epicsAccessService.GetAvailable(
            authData, 
            epics => map(epics.SelectMany(e => e.Issues!)),
            cancellationToken);
    }
}