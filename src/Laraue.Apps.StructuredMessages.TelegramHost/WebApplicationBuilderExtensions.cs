using Laraue.Apps.StructuredMessages.DataAccess;
using Laraue.Apps.StructuredMessages.DataAccess.Models;
using Laraue.Apps.StructuredMessages.Services;
using Laraue.Apps.StructuredMessages.TelegramServices;
using Laraue.Core.DataAccess.Linq2DB.Extensions;
using Laraue.Telegram.NET.Authentication.Extensions;
using Laraue.Telegram.NET.Core;
using Laraue.Telegram.NET.Core.Extensions;
using Laraue.Telegram.NET.Core.Middleware;
using Laraue.Telegram.NET.Core.Routing.Middleware;
using Laraue.Telegram.NET.UpdatesQueue.EFCore.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Laraue.Apps.StructuredMessages.TelegramHost;

public static class WebApplicationBuilderExtensions
{
    extension(WebApplicationBuilder builder)
    {
        public WebApplicationBuilder AddTelegramOptions(string sectionName)
        {
            builder.Services.AddOptions<TelegramNetOptions>();
            builder.Services.Configure<TelegramNetOptions>(
                builder.Configuration.GetSection(sectionName));
            
            return builder;
        }
        
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
        
        public WebApplicationBuilder AddApplicationServices()
        {
            builder.Services
                .AddTelegramCore()
                .AddEfCoreUpdatesQueue<DatabaseContext>()
                .AddTelegramMiddleware<HandleExceptionsMiddleware>()
                .AddTelegramMiddleware<AutoCallbackResponseMiddleware>()
                .AddTelegramMiddleware<HandleAllMessagesMiddleware>()
                .AddTelegramAuthentication<User, Guid, TelegramUserQueryService, RequestContext>();

            builder.Services
                .AddScoped<ITelegramMessageService, TelegramMessageService>()
                .AddScoped<ITelegramMessageCategoryService, TelegramMessageCategoryService>();

            builder.Services
                .AddScoped<IMessageService, MessageService>()
                .AddScoped<IMessageCategoryService, MessageCategoryService>();
            
            builder.Services.AddControllers();

            return builder;
        }

        private string? GetConnection(string connectionStringName)
        {
            return builder.Configuration.GetConnectionString(connectionStringName);
        }
    }
}