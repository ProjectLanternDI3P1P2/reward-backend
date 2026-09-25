using Reward.Presentation.Extensions;
using Reward.Application;
using Reward.Infrastructure;
using Reward.Infrastructure.Persistence.Seeding;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureApi();

builder.Services
    .AddInfrastructureServices(builder.Configuration)
    .AddApplicationServices();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await app.Services.MigrateAndSeedDevelopmentDataAsync();
}

app.ConfigureStart();

await app.RunAsync();

public partial class Program;
