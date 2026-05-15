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
                .AddScoped<IAccessService, AccessService>()
                .AddScoped<ICoreIssuesService, CoreIssuesService>()
                .AddScoped<IIssuesAccessService, IssuesAccessService>()
                .AddScoped<ICoreEpicsService, CoreEpicsService>()
                .AddScoped<IEpicsAccessService, EpicsAccessService>()
                .AddScoped<ICoreStatusService, CoreStatusService>()
                .AddScoped<IStatusAccessService, StatusAccessService>()
                .AddScoped<ICoreUserPreferencesService, CoreUserPreferencesService>()
                .AddScoped<ICoreUserOrganizationPreferencesService, CoreUserOrganizationPreferencesService>()
                .AddScoped<ICoreSpacesService, CoreSpacesService>()
                .AddScoped<ISpacesAccessService, SpacesAccessService>()
                .AddScoped<ICoreOrganizationsService, CoreOrganizationsService>()
                .AddScoped<IOrganizationAccessService, OrganizationAccessService>()
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