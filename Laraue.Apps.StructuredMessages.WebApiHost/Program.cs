using Laraue.Apps.StructuredMessages.DataAccess;
using Laraue.Apps.StructuredMessages.Services;
using Laraue.Apps.StructuredMessages.WebApiHost;
using Laraue.Core.DataAccess.Linq2DB.Extensions;    
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

const string dbConnectionStringName = "Postgre";

builder
    .AddApplicationServices()
    .AddDatabaseServices(dbConnectionStringName);

var app = builder.Build();

app.MapControllers();
app.Services.UseLinq2Db();

using (var scope = app.Services.CreateScope())
{
    await using var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
    await db.Database.MigrateAsync();
}

var origins = builder
    .Configuration
    .GetRequiredSection("Cors:Hosts")
    .Get<string[]>();

app.UseCors(corsPolicyBuilder =>
    corsPolicyBuilder.WithOrigins(origins)
        .AllowCredentials()
        .AllowAnyMethod()
        .AllowAnyHeader());

app.Run();