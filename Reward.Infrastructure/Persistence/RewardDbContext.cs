using Reward.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Reward.Infrastructure.Persistence;

public class RewardDbContext(DbContextOptions<RewardDbContext> options) : DbContext(options)
{
    public virtual DbSet<Player> Players { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Applique toutes les configurations d'entités automatiquement
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RewardDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
