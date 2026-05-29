using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Laraue.Apps.StructuredMessages.Services;

public static class DatabaseFacadeExtensions
{
    public static void EnsureTransactionStarted(this DatabaseFacade facade)
    {
        if (facade.CurrentTransaction == null)
            throw new InvalidOperationException("Database transaction is required.");
    }
}