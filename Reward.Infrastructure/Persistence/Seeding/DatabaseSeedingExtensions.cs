using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Reward.Infrastructure.Persistence.Seeding;

public static class DatabaseSeedingExtensions
{
    public static async Task MigrateAndSeedDevelopmentDataAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        RewardDbContext context = scope.ServiceProvider.GetRequiredService<RewardDbContext>();

        await context.Database.MigrateAsync(cancellationToken);
        await DataSeeder.SeedAsync(context, cancellationToken);
    }
}
