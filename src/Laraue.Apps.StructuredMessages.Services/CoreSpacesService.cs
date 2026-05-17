using Laraue.Apps.StructuredMessages.DataAccess;
using Laraue.Apps.StructuredMessages.DataAccess.Models;
using Laraue.Core.DataAccess.EFCore.Extensions;
using Laraue.Core.DateTime.Services.Abstractions;
using Laraue.Core.Exceptions.Web;
using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace Laraue.Apps.StructuredMessages.Services;

public interface ICoreSpacesService
{
    Task<long> Create(
        long organizationId,
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
    
    /// <summary>
    /// Move space to the new organization.
    /// </summary>
    Task Move(
        long spaceId,
        long newOrganizationId,
        CancellationToken cancellationToken);
    
    /// <summary>
    /// Move space epics to the new space of any organization.
    /// </summary>
    Task MoveEpics(
        long spaceId,
        long newSpaceId,
        CancellationToken cancellationToken);
}

public class CoreSpacesService(
    DatabaseContext context,
    IDateTimeProvider dateTimeProvider)
    : ICoreSpacesService
{
    public async Task<long> Create(
        long organizationId,
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
            OrganizationId = organizationId,
            Epics = new List<Epic>
            {
                OrganizationDefaults.GetNewBacklogEpicEntity(creatorId, dateTime)
            }
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
        var defaultSpace = await context.Spaces
            .Where(x => x.Id == id)
            .Select(x => x.Organization!)
            .Select(o => o.Spaces!.First(y => y.IsDefault))
            .Select(s => new
            {
                SpaceId = s.Id, 
                NewStatusId = (long?)s.Epics!.FirstOrDefault(e => e.IsDefault)!.Statuses!.OrderBy(o => o.SortOrder).FirstOrDefault()!.Id, // Status should be taken from FE in future iterations
            })
            .FirstOrDefaultAsyncEF(cancellationToken);
        
        if (defaultSpace is null)
            throw new NotFoundException($"Default Organization Space for Space:{id} is not found");
            
        if (defaultSpace.SpaceId == id)
            throw new ForbiddenException("Default Space can not be deleted");
        
        // The situation should not happen in real App as soon as Backlog Epic is always required for Space. This line will be dropped when status id will be taken from FE. 
        if (defaultSpace.NewStatusId is null)
            throw new BadRequestException(nameof(id), $"Backlog or default status for Backlog was not found in Default Space to move issues from deleting Space:{id}");
        
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        await context.Issues
            .Where(x => x.Status!.Epic!.SpaceId == id)
            .ExecuteUpdateAsync(u => u
                .SetProperty(p => p.StatusId, defaultSpace.NewStatusId),
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

    public async Task Move(long spaceId, long newOrganizationId, CancellationToken cancellationToken)
    {
        var sourceData = await context.Spaces
            .Where(x => x.Id == spaceId)
            .Select(x => new { x.IsDefault })
            .FirstOrThrowNotFoundEFAsync($"Space: {spaceId} is not found", cancellationToken);
        
        if (sourceData.IsDefault)
            throw new ForbiddenException("Default space cannot be moved. Move space epics instead.");

        await context.Spaces
            .Where(x => x.Id == spaceId)
            .ExecuteUpdateAsync(u => u
                .SetProperty(p => p.OrganizationId, newOrganizationId),
                cancellationToken);
    }

    public async Task MoveEpics(long spaceId, long newSpaceId, CancellationToken cancellationToken)
    {
        await context.Epics
            .Where(x => x.SpaceId == spaceId)
            .ExecuteUpdateAsync(u => u
                .SetProperty(epic => epic.SpaceId, newSpaceId),
                cancellationToken);
    }
}