using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Reward.Infrastructure.Persistence;

/// <summary>Creates the context for EF Core commands without starting the HTTP application.</summary>
public sealed class RewardDbContextFactory : IDesignTimeDbContextFactory<RewardDbContext>
{
    public RewardDbContext CreateDbContext(string[] args)
    {
        string configurationDirectory = FindPresentationConfigurationDirectory();
        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(configurationDirectory)
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        string connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection is required to create EF Core migrations.");

        DbContextOptions<RewardDbContext> options = new DbContextOptionsBuilder<RewardDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new RewardDbContext(options);
    }

    private static string FindPresentationConfigurationDirectory()
    {
        for (DirectoryInfo? directory = new(Directory.GetCurrentDirectory()); directory is not null; directory = directory.Parent)
        {
            string presentationDirectory = Path.Combine(directory.FullName, "Reward.Presentation");
            if (File.Exists(Path.Combine(presentationDirectory, "appsettings.json")))
            {
                return presentationDirectory;
            }
        }

        throw new InvalidOperationException("Could not find Reward.Presentation/appsettings.json from the current directory.");
    }
}
