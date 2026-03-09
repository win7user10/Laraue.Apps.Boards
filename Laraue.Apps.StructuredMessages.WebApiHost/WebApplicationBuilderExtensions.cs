using Laraue.Apps.StructuredMessages.Services;
using Laraue.Apps.StructuredMessages.WebApiServices;

namespace Laraue.Apps.StructuredMessages.WebApiHost;

public static class WebApplicationBuilderExtensions
{
    extension(WebApplicationBuilder builder)
    {
        public WebApplicationBuilder AddApplicationServices()
        {
            builder.AddCoreServices();

            builder.Services
                .AddScoped<IMessagesService, MessagesService>()
                .AddScoped<ICategoriesService, CategoriesService>();
            
            builder.Services.AddControllers();

            return builder;
        }
    }
}