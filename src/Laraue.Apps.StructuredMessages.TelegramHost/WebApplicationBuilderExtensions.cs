using Laraue.Apps.StructuredMessages.DataAccess;
using Laraue.Apps.StructuredMessages.DataAccess.Models;
using Laraue.Apps.StructuredMessages.Services;
using Laraue.Apps.StructuredMessages.TelegramServices;
using Laraue.Apps.StructuredMessages.TelegramServices.Interceptors;
using Laraue.Apps.StructuredMessages.TelegramServices.Services;
using Laraue.Apps.StructuredMessages.TelegramServices.Services.Categories;
using Laraue.Apps.StructuredMessages.TelegramServices.Services.Messages;
using Laraue.Core.DataAccess.Linq2DB.Extensions;
using Laraue.Telegram.NET.Authentication.Extensions;
using Laraue.Telegram.NET.Core;
using Laraue.Telegram.NET.Core.Extensions;
using Laraue.Telegram.NET.Core.Middleware;
using Laraue.Telegram.NET.Core.Routing.Middleware;
using Laraue.Telegram.NET.Interceptors.EFCore.Extensions;
using Laraue.Telegram.NET.UpdatesQueue.EFCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

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
        
        public WebApplicationBuilder AddApplicationServices()
        {
            builder.AddCoreServices();
            
            builder.Services.AddOptions<MiniAppOptions>();
            builder.Services.Configure<MiniAppOptions>(
                builder.Configuration.GetSection(nameof(MiniAppOptions)));
            
            builder.Services
                .AddTelegramCore()
                .AddTelegramRequestEfCoreInterceptors<Guid, DatabaseContext>(
                    [typeof(CreateCategoryFromMessageInterceptor).Assembly])
                .AddEfCoreUpdatesQueue<DatabaseContext>()
                .AddTelegramMiddleware<HandleExceptionsMiddleware>()
                .AddTelegramMiddleware<AutoCallbackResponseMiddleware>()
                .AddTelegramMiddleware<HandleAllMessagesMiddleware>()
                .AddTelegramAuthentication<User, Guid, TelegramUserQueryService, RequestContext>();

            builder.Services
                .AddScoped<ITelegramMessageService, TelegramMessageService>()
                .AddScoped<ITelegramMessageServiceRepository, TelegramMessageServiceRepository>()
                .AddScoped<ITelegramMessageCategoryService, TelegramMessageCategoryService>();

            builder.Services
                .AddScoped<ICoreMessageService, CoreMessageService>()
                .AddScoped<ICoreCategoryService, CoreCategoryService>()
                .AddScoped<ICoreStatusService, CoreStatusService>();
            
            builder.Services.AddControllers();

            return builder;
        }
    }
}