using System.Runtime.CompilerServices;
using Laraue.Apps.StructuredMessages.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Laraue.Apps.StructuredMessages.Services;

public interface ISpaceCounterService
{
    Task<int> GetNextNumber(long spaceId, CancellationToken cancellationToken);
}

public class SpaceCounterService(DatabaseContext context) : ISpaceCounterService
{
    public Task<int> GetNextNumber(long spaceId, CancellationToken cancellationToken)
    {
        var sqlQuery = @"
            INSERT INTO space_counters (space_id, last_number)
            VALUES ({0}, 1)
            ON CONFLICT (space_id) DO UPDATE
            SET last_number = space_counters.last_number + 1
            RETURNING last_number";
        
        var query = FormattableStringFactory.Create(sqlQuery, spaceId);
        return context.Database.ExecuteSqlAsync(query, cancellationToken);
    }
}