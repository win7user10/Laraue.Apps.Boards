using Laraue.Apps.StructuredMessages.DataAccess;
using Laraue.Apps.StructuredMessages.DataAccess.Models;
using Laraue.Core.DataAccess.Linq2DB.Extensions;
using Laraue.Telegram.NET.Authentication.Extensions;
using Laraue.Telegram.NET.Core.Extensions;
using Laraue.Telegram.NET.Core.Middleware;
using Laraue.Telegram.NET.Core.Routing.Middleware;
using Laraue.Telegram.NET.Interceptors.EFCore.Extensions;
using Laraue.Telegram.NET.UpdatesQueue.EFCore.Extensions;
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
                .AddScoped<IMessageService, MessageService>()
                .AddScoped<IMessageCategoryService, MessageCategoryService>()
                .AddScoped<IMessageStatusService, MessageStatusService>();

            return builder;
        }

        private string? GetConnection(string connectionStringName)
        {
            return builder.Configuration.GetConnectionString(connectionStringName);
        }
    }
}