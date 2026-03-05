using Laraue.Apps.StructuredMessages.DataAccess.Models;
using Laraue.Telegram.NET.UpdatesQueue.EFCore;
using Laraue.Telegram.NET.UpdatesQueue.EFCore.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Laraue.Apps.StructuredMessages.DataAccess;

public class DatabaseContext : DbContext, IUpdatesQueueDbContext
{
    public DatabaseContext(DbContextOptions options) 
        : base(options)
    {
    }
    
    public DbSet<User> Users { get; init; }
    public DbSet<Message> Messages { get; init; }
    public DbSet<MessageType> MessageTypes { get; init; }
    public DbSet<MessageTypeStatus> MessageTypeStatuses { get; init; }

    public DbSet<Update> Updates { get; set; }
    
    public DbSet<FailedUpdate> FailedUpdates { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("uuid-ossp");
    }
}