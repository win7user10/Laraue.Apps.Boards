using Laraue.Apps.StructuredMessages.DataAccess;
using Laraue.Apps.StructuredMessages.DataAccess.Models;
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
        
        // Cleanup before test run
        Database.DirectSpacePermissions.ExecuteDelete();
        Database.DirectEpicPermissions.ExecuteDelete();
        Database.SpaceCounters.ExecuteDelete();
        Database.Users.ExecuteDelete();
    }

    public void Dispose()
    {
        _scope.Dispose();
    }
    
    public async Task<Guid> CreateUser(Action<User>? setupUser = null)
    {
        var user = new User();
        
        setupUser?.Invoke(user);
        
        Database.Users.Add(user);
        
        await Database.SaveChangesAsync();
        
        return user.Id;
    }

    public Task<Organization> InitializePersonalOrganization(Guid userId, Action<OrganizationInitializer>? setupInitializer = null)
    {
        return InitializeOrganization(userId, (initializer) =>
        {
            initializer.SetIsPersonal(true);
            setupInitializer?.Invoke(initializer);
        });
    }
    

    public Task<Organization> InitializeOrganization(Guid userId, Action<OrganizationInitializer>? setupInitializer = null)
    {
        var initializer = ActivatorUtilities.CreateInstance<OrganizationInitializer>(_scope.ServiceProvider, userId);
        
        initializer
            .WithName("New Org")
            .SetIsPersonal(false)
            .WithTimestamp(DateTime.UtcNow);

        setupInitializer?.Invoke(initializer);
        
        return initializer.Initialize();
    }
}