using Microsoft.EntityFrameworkCore;
using Reward.Application;
using Reward.Infrastructure;
using Reward.Infrastructure.Persistence;
using Reward.Infrastructure.Persistence.Seeding;
using Reward.Presentation.Extensions;

bool migrateOnly = args.Contains("--migrate", StringComparer.Ordinal);
var builder = WebApplication.CreateBuilder(
    args.Where(argument => argument != "--migrate").ToArray()
);

builder.ConfigureApi();

builder.Services.AddInfrastructureServices(builder.Configuration).AddApplicationServices();

var app = builder.Build();

if (migrateOnly)
{
    await using (AsyncServiceScope scope = app.Services.CreateAsyncScope())
    {
        RewardDbContext database = scope.ServiceProvider.GetRequiredService<RewardDbContext>();
        if (!database.Database.GetMigrations().Any())
        {
            throw new InvalidOperationException(
                "No database migrations are packaged in this image."
            );
        }

        await database.Database.MigrateAsync();
    }
    app.Logger.LogInformation("Database migrations completed; exiting without starting the API.");
    await app.DisposeAsync();
    return;
}

if (app.Environment.IsDevelopment())
{
    await app.Services.MigrateAndSeedDevelopmentDataAsync();
}

app.ConfigureStart();

await app.RunAsync();

public partial class Program;
