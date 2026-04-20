using Laraue.Apps.StructuredMessages.DataAccess;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using User = Laraue.Apps.StructuredMessages.DataAccess.Models.User;

namespace Laraue.Apps.StructuredMessages.IntegrationTests.Infrastructure;

public class WebApiTestHost
    : WebApplicationFactory<Program>
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(config =>
        {
            config.AddJsonFile("appsettings.json", optional: true);
        });

        return base.CreateHost(builder);
    }

    public Proxy<TController> Controller<TController>() where TController : ControllerBase
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        
        return new Proxy<TController>(client, this);
    }

    public WebApiTestHostScope CreateTestScope()
    {
        var scope = Services.CreateScope();
        
        return new WebApiTestHostScope(scope);
    }
}

public class WebApiTestHostScope : IDisposable
{
    private readonly IServiceScope _scope;
    public DatabaseContext Database => _scope.ServiceProvider.GetRequiredService<DatabaseContext>();

    public WebApiTestHostScope(IServiceScope scope)
    {
        _scope = scope;
        Database.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        Database.Users.ExecuteDelete(); // Cleanup before test run
    }

    public void Dispose()
    {
        _scope.Dispose();
    }
    
    public async Task<Guid> CreateUser()
    {
        var user = new User();
        
        Database.Users.Add(user);
        
        await Database.SaveChangesAsync();
        
        return user.Id;
    }
}