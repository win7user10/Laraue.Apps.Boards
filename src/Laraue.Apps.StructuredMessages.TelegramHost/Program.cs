using Laraue.Apps.StructuredMessages.DataAccess;
using Laraue.Apps.StructuredMessages.TelegramHost;
using Laraue.Core.DataAccess.Linq2DB.Extensions;
using Laraue.Telegram.NET.Core.Extensions;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

const string dbConnectionStringName = "Postgre";

builder
    .AddTelegramOptions("Telegram")
    .AddApplicationServices()
    .AddDatabaseServices(dbConnectionStringName);

var app = builder.Build();

app.Services.UseLinq2Db();

using (var scope = app.Services.CreateScope())
{
    await using var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
    await db.Database.MigrateAsync();
    
    app.MapTelegramRequests();
}

app.Run();