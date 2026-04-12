using Laraue.Apps.StructuredMessages.Services;
using Laraue.Apps.StructuredMessages.WebApiServices;
using Laraue.Core.DateTime.Services.Abstractions;
using Laraue.Core.DateTime.Services.Impl;
using Laraue.Core.Exceptions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Telegram.Bot;

namespace Laraue.Apps.StructuredMessages.WebApiHost;

public static class WebApplicationBuilderExtensions
{
    extension(WebApplicationBuilder builder)
    {
        public WebApplicationBuilder AddApplicationServices()
        {
            builder.AddCoreServices();
            builder.Services.AddHttpClient();

            builder.Services
                .AddScoped<ITelegramAuthService, TelegramAuthService>()
                .AddSingleton<ITelegramBotClient, TelegramBotClient>(
                    sp => new TelegramBotClient(sp.GetRequiredService<IOptions<TelegramOptions>>().Value.Token));

            builder.Services
                .AddScoped<IIssuesService, IssuesService>()
                .AddScoped<IEpicsService, EpicsService>()
                .AddScoped<IStatusesService, StatusesService>()
                .AddScoped<IUserPreferencesService, UserPreferencesService>();

            builder.Services
                .AddSingleton<IDateTimeProvider, DateTimeProvider>()
                .AddScoped<ExceptionHandleMiddleware>();
            
            builder.Services.AddControllers();

            return builder;
        }
        
        public WebApplicationBuilder AddAuthentication()
        {
            builder.Services.AddOptions<AuthOptions>();
            builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection("Auth"));
            
            builder.Services.AddSingleton<IAuthService, AuthService>();
            builder.Services.AddAuthentication()
                .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    var key = builder.Configuration["Auth:Key"]
                        ?? throw new InvalidOperationException("Auth:Key is required.");

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = AuthService.Issuer,
                        ValidateAudience = true,
                        ValidAudience = AuthService.Audience,
                        IssuerSigningKey = AuthService.GetSymmetricSecurityKey(key),
                        ValidateIssuerSigningKey = true,
                        ValidateLifetime = false,
                    };
                });

            return builder;
        }
    }
}