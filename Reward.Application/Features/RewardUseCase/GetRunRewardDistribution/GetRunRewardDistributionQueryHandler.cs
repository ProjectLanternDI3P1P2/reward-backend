using MediatR;
using Reward.Application.Ports;
using Reward.Domain.Entities;
using Reward.Domain.Enums;
using Reward.Domain.Exceptions;
using Reward.Domain.Repositories;
using RewardEntity = Reward.Domain.Entities.Reward;

namespace Reward.Application.Features.RewardUseCase.GetRunRewardDistribution;

public sealed class GetRunRewardDistributionQueryHandler(
    IRewardRepository repository,
    IPlayerParticipationClient participationClient
) : IRequestHandler<GetRunRewardDistributionQuery, RunRewardDistribution>
{
    public async Task<RunRewardDistribution> Handle(
        GetRunRewardDistributionQuery request,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(request.ExternalUserId))
        {
            throw new UnauthenticatedException("The authenticated user identity is missing.");
        }

        // Only the participants of the run may see who received what. Anyone else gets the
        // same answer as for an unknown run, so the existence of the run is not disclosed.
        bool isParticipant = await participationClient.IsRunParticipantAsync(
            request.ExternalUserId,
            request.DungeonRunId,
            cancellationToken
        );
        if (!isParticipant)
        {
            throw new KeyNotFoundException(
                $"No reward distribution is available for run '{request.DungeonRunId}'."
            );
        }

        IReadOnlyList<RewardEntity> rewards = await repository.GetGroupRewardsByRunAsync(
            request.DungeonRunId,
            cancellationToken
        );

        List<RewardDistributionCause> distributions = rewards
            .GroupBy(reward =>
                (SourceType: reward.RewardSource.Name, CauseId: reward.CauseId!.Value)
            )
            .OrderBy(group => group.Min(reward => reward.CreatedAt))
            .ThenBy(group => group.Key.CauseId)
            .Select(group => new RewardDistributionCause(
                group.Key.SourceType,
                group.Key.CauseId,
                ToDistributedRewards(group)
            ))
            .ToList();

        return new RunRewardDistribution(request.DungeonRunId, distributions);
    }

    // A shared reward is stored once per hero; it is shown once, with all its recipients.
    private static List<DistributedReward> ToDistributedRewards(
        IEnumerable<RewardEntity> rewards
    ) =>
        rewards
            .SelectMany(reward => reward.Items.Select(item => (Reward: reward, Item: item)))
            .GroupBy(entry => (entry.Reward.Distribution, entry.Item.ItemId, entry.Item.Quantity))
            .Select(group =>
            {
                RewardItem first = group.First().Item;
                return new DistributedReward(
                    group.Key.Distribution.ToCode(),
                    group.Key.ItemId,
                    first.ItemNameSnapshot,
                    first.ItemRaritySnapshot,
                    group.Key.Quantity,
                    group
                        .OrderBy(entry => entry.Reward.CreatedAt)
                        .ThenBy(entry => entry.Reward.HeroId)
                        .Select(entry => new RewardRecipient(
                            entry.Reward.HeroId,
                            entry.Reward.RecipientPlayerId,
                            entry.Item.ItemInstanceId
                        ))
                        .ToList()
                );
            })
            .OrderBy(reward => reward.Name, StringComparer.Ordinal)
            .ThenBy(reward => reward.Kind, StringComparer.Ordinal)
            .ThenBy(reward => reward.ItemId)
            .ToList();
}
