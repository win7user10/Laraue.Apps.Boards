using Laraue.Apps.StructuredMessages.DataAccess.Models;
using Laraue.Telegram.NET.Interceptors.EFCore;
using Laraue.Telegram.NET.UpdatesQueue.EFCore;
using Laraue.Telegram.NET.UpdatesQueue.EFCore.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Laraue.Apps.StructuredMessages.DataAccess;

public class DatabaseContext : DbContext, IUpdatesQueueDbContext, IInterceptorsDbContext<Guid>
{
    public DatabaseContext(DbContextOptions options) 
        : base(options)
    {
    }
    
    public DbSet<User> Users { get; init; }
    public DbSet<Card> Cards { get; init; }
    public DbSet<CardCategory> CardCategories { get; init; }
    public DbSet<CardStatus> CardStatuses { get; init; }
    public DbSet<TelegramFile> TelegramFiles { get; init; }
    public DbSet<TelegramMessagePhoto> TelegramPhotos { get; init; }
    public DbSet<TelegramMessageVideo> TelegramVideos { get; init; }
    public DbSet<TelegramMessage> TelegramMessages { get; init; }
    public DbSet<TelegramMediaGroup> TelegramMediaGroups { get; init; }

    public DbSet<Update> Updates { get; set; }
    public DbSet<FailedUpdate> FailedUpdates { get; set; }
    public DbSet<InterceptorStateModel<Guid>> InterceptorState { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("uuid-ossp");
        modelBuilder.HasPostgresExtension("pg_trgm");
        
        modelBuilder.Entity<Card>(entity =>
        {
            entity
                .HasIndex(x => x.Content)
                .HasMethod("gin")
                .HasOperators("gin_trgm_ops");
        });
        
        modelBuilder.Entity<TelegramFile>(entity =>
        {
            entity
                .HasIndex(x => x.FileUniqueId)
                .IsUnique();
        });
        
        modelBuilder.Entity<TelegramMediaGroup>(entity =>
        {
            entity
                .HasIndex(x => x.ExternalId)
                .IsUnique();
        });
        
        modelBuilder.Entity<TelegramMessage>(entity =>
        {
            entity
                .HasIndex(x => new { x.ExternalMessageId, x.ExternalChatId })
                .IsUnique();
        });
    }
}