using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Reward.Infrastructure.Persistence;

namespace Reward.Test.Integration;

public sealed class RewardWebApplicationFactory(string connectionString)
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration(configuration =>
        {
            configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = connectionString,
                    ["RabbitMq:Enabled"] = "false",
                }
            );
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<RewardDbContext>>();
            services.RemoveAll<RewardDbContext>();
            services.AddDbContext<RewardDbContext>(options => options.UseNpgsql(connectionString));
        });
    }
}
