using Laraue.Apps.StructuredMessages.DataAccess;
using Laraue.Apps.StructuredMessages.DataAccess.Models;
using Laraue.Core.DateTime.Services.Abstractions;
using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace Laraue.Apps.StructuredMessages.Services;

public interface ICoreSpacesService
{
    Task<long> Create(
        Guid creatorId,
        string name,
        string color,
        CancellationToken cancellationToken);
    
    Task Update(
        long id,
        Action<UpdateSettersBuilder<Space>> setters,
        CancellationToken cancellationToken);
    
    Task Delete(
        long id,
        CancellationToken cancellationToken);
    
    Task<bool> UserHasAccessToSpace(
        Guid userId,
        long spaceId,
        AccessType accessType,
        CancellationToken cancellationToken);
}

public class CoreSpacesService(
    DatabaseContext context,
    IDateTimeProvider dateTimeProvider)
    : ICoreSpacesService
{
    public async Task<long> Create(
        Guid creatorId,
        string name,
        string color,
        CancellationToken cancellationToken)
    {
        var dateTime = dateTimeProvider.UtcNow;
        
        var entity = new Space
        {
            CreatorId = creatorId,
            Name = name,
            Color = color,
            CreatedAt = dateTime,
            UpdatedAt = dateTime,
        };
        
        context.Spaces.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        
        return entity.Id;
    }

    public Task Update(long id, Action<UpdateSettersBuilder<Space>> setters, CancellationToken cancellationToken)
    {
        var date = dateTimeProvider.UtcNow;
        
        return context.Spaces
            .Where(x => x.Id == id)
            .ExecuteUpdateAsync(
                update =>
                {
                    setters(update);
                    update
                        .SetProperty(p => p.UpdatedAt, date);
                },
                cancellationToken);
    }

    public async Task Delete(long id, CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        await context.Issues
            .Where(x => x.SpaceId == id)
            .ExecuteUpdateAsync(u => u
                .SetProperty(p => p.SpaceId, (long?)null)
                .SetProperty(p => p.EpicId, (long?)null)
                .SetProperty(p => p.StatusId, (long?)null),
                cancellationToken);
        
        await context.Statuses
            .Where(x => x.Epic!.Space!.Id == id)
            .ExecuteDeleteAsync(cancellationToken);
        
        await context.Epics
            .Where(c => c.SpaceId == id)
            .ExecuteDeleteAsync(cancellationToken);
        
        await context.Spaces
            .Where(c => c.Id == id)
            .ExecuteDeleteAsync(cancellationToken);
        
        await transaction.CommitAsync(cancellationToken);
    }

    public Task<bool> UserHasAccessToSpace(
        Guid userId,
        long spaceId,
        AccessType accessType,
        CancellationToken cancellationToken)
    {
        return context.Spaces
            .Where(x => x.CreatorId == userId)
            .Where(x => x.Id == spaceId)
            .AnyAsyncEF(cancellationToken);
    }
}