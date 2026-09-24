using Reward.Domain.Entities;

namespace Reward.Domain.Repositories;

public interface IPlayerRepository
{
    Task<Player?> GetPlayerByIdAsync(Guid playerId, CancellationToken cancellationToken);
    Task AddPlayerAsync(Player player, CancellationToken cancellationToken);
}
