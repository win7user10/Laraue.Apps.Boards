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
    public DbSet<Message> Messages { get; init; }
    public DbSet<MessageCategory> MessageCategories { get; init; }
    public DbSet<MessageStatus> MessageStatuses { get; init; }
    public DbSet<TelegramFile> TelegramFiles { get; init; }
    public DbSet<MessageTelegramPhoto> TelegramPhotos { get; init; }
    public DbSet<MessageTelegramVideo> TelegramVideos { get; init; }

    public DbSet<Update> Updates { get; set; }
    public DbSet<FailedUpdate> FailedUpdates { get; set; }
    public DbSet<InterceptorStateModel<Guid>> InterceptorState { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("uuid-ossp");
        modelBuilder.HasPostgresExtension("pg_trgm");
        
        modelBuilder.Entity<Message>(entity =>
        {
            entity
                .HasIndex(x => x.Content)
                .HasMethod("gin")
                .HasOperators("gin_trgm_ops");
            
            entity
                .HasIndex(x => x.TelegramMediaGroupId)
                .IsUnique();
        });
        
        modelBuilder.Entity<TelegramFile>(entity =>
        {
            entity
                .HasIndex(x => x.FileUniqueId)
                .IsUnique();
        });
    }
}