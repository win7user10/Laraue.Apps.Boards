using Laraue.Apps.StructuredMessages.DataAccess;
using Laraue.Core.DataAccess.Linq2DB.Extensions;
using Laraue.Core.DateTime.Services.Abstractions;
using Laraue.Core.DateTime.Services.Impl;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Laraue.Apps.StructuredMessages.Services;

public static class WebApplicationBuilderExtensions
{
    extension(WebApplicationBuilder builder)
    {
        public WebApplicationBuilder AddDatabaseServices(string connectionStringName)
        {
            var connection = GetConnection(builder, connectionStringName);
            
            builder.Services
                .AddDbContext<DatabaseContext>(opt =>
                {
                    opt
                        .UseNpgsql(connection)
                        .UseSnakeCaseNamingConvention();
                })
                .AddLinq2Db();

            return builder;
        }
        
        public WebApplicationBuilder AddCoreServices()
        {
            builder.Services
                .AddSingleton<IDateTimeProvider, DateTimeProvider>()
                .AddScoped<ICoreIssuesService, CoreIssuesService>()
                .AddScoped<ICoreEpicsService, CoreEpicsService>()
                .AddScoped<ICoreStatusService, CoreStatusService>()
                .AddScoped<ICoreUserPreferencesService, CoreUserPreferencesService>()
                .AddScoped<ICoreSpacesService, CoreSpacesService>()
                .AddSingleton<IFileStorage, FileStorage>();

            builder.Services.AddMemoryCache();
            
            builder.Services.AddOptions<FileStorageOptions>();
            builder.Services.Configure<FileStorageOptions>(
                builder.Configuration.GetSection(nameof(FileStorageOptions)));

            return builder;
        }

        private string? GetConnection(string connectionStringName)
        {
            return builder.Configuration.GetConnectionString(connectionStringName);
        }
    }
}