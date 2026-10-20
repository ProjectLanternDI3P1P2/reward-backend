using Microsoft.EntityFrameworkCore;
using Reward.Domain.Entities;
using Reward.Domain.Repositories;

namespace Reward.Infrastructure.Persistence.Repositories;

public sealed class PlaceholderRepository(RewardDbContext dbContext) : IPlaceholderRepository
{
    public Task<Placeholder?> GetByIdAsync(
        Guid placeholderId,
        CancellationToken cancellationToken
    ) =>
        dbContext.Placeholders.SingleOrDefaultAsync(
            placeholder => placeholder.Id == placeholderId,
            cancellationToken
        );
}
