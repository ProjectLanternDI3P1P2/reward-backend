using Reward.Presentation.Extensions;
using Reward.Application;
using Reward.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureApi();

builder.Services
    .AddInfrastructureServices(builder.Configuration)
    .AddApplicationServices();

var app = builder.Build();

app.ConfigureStart();

await app.RunAsync();
