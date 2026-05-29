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
    private const string SqlQuery = @"
            INSERT INTO space_counters (space_id, last_number)
            VALUES ({0}, 1)
            ON CONFLICT (space_id) DO UPDATE
            SET last_number = space_counters.last_number + 1
            RETURNING last_number";

    public async Task<int> GetNextNumber(long spaceId, CancellationToken cancellationToken)
    {
        context.Database.EnsureTransactionStarted();
        
        var query = FormattableStringFactory.Create(SqlQuery, spaceId);
        var result = await context.Database
            .SqlQuery<int>(query)
            .ToListAsync(cancellationToken);

        return result.First();
    }
}