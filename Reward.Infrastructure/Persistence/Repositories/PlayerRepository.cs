using Reward.Domain.Entities;
using Reward.Domain.Repositories;

namespace Reward.Infrastructure.Persistence.Repositories;

public sealed class PlayerRepository(RewardDbContext dbContext) : IPlayerRepository
{
    public async Task AddPlayerAsync(Player player, CancellationToken cancellationToken)
    {
        await dbContext.Players.AddAsync(player, cancellationToken);
    }

    public async Task<Player?> GetPlayerByIdAsync(Guid playerId, CancellationToken cancellationToken)
    {
        return await dbContext.Players.FindAsync([playerId], cancellationToken);
    }
}
