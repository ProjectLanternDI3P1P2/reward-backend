using MediatR;
using Reward.Domain.Entities;
using Reward.Domain.Enums;
using Reward.Domain.Repositories;
using Reward.Domain.Services;
using ILogger = Serilog.ILogger;
using RewardEntity = Reward.Domain.Entities.Reward;

namespace Reward.Application.Features.RewardUseCase.GrantReward;

public sealed class GrantRewardCommandHandler(
    IRewardRepository rewardRepository,
    IInventoryRepository inventoryRepository,
    IClock clock,
    ILogger logger
) : IRequestHandler<GrantRewardCommand, GrantRewardResult>
{
    public async Task<GrantRewardResult> Handle(
        GrantRewardCommand request,
        CancellationToken cancellationToken
    )
    {
        string rewardKey = RewardEntity.CreateKey(
            request.SourceType,
            request.CauseId,
            request.HeroId
        );

        RewardEntity? existing = await rewardRepository.GetByKeyAsync(rewardKey, cancellationToken);
        if (existing is not null)
        {
            return AcknowledgeDuplicate(existing, request);
        }

        Inventory inventory =
            await inventoryRepository.GetByHeroIdForUpdateAsync(request.HeroId, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"Inventory for hero '{request.HeroId}' was not found."
            );

        // A concurrent delivery of the same operation may have been granted while this one
        // was waiting for the inventory lock.
        existing = await rewardRepository.GetByKeyAsync(rewardKey, cancellationToken);
        if (existing is not null)
        {
            return AcknowledgeDuplicate(existing, request);
        }

        string sourceName = request.SourceType.ToCode();
        RewardSource source =
            await rewardRepository.GetSourceByNameAsync(sourceName, cancellationToken)
            ?? throw new InvalidOperationException(
                $"The reward source '{sourceName}' is not configured."
            );

        DateTimeOffset now = clock.UtcNow;
        var reward = new RewardEntity
        {
            Id = Guid.NewGuid(),
            HeroId = request.HeroId,
            RunId = request.RunId,
            RewardSourceId = source.Id,
            RewardSource = source,
            Type = RewardType.Item,
            Status = RewardStatus.Pending,
            RewardKey = rewardKey,
            CreatedAt = now,
        };

        foreach (GrantRewardItem line in request.Items)
        {
            Item item =
                await inventoryRepository.GetItemByIdAsync(line.ItemId, cancellationToken)
                ?? throw new KeyNotFoundException($"Item '{line.ItemId}' was not found.");

            ItemInstance itemInstance = reward.CreditItem(inventory, item, line.Quantity, now);
            inventoryRepository.AddItemInstance(itemInstance);
        }

        // The reward and its items are committed in the same transaction, so a persisted
        // reward is always fully applied.
        reward.Status = RewardStatus.Applied;
        rewardRepository.Add(reward);

        logger.Information(
            "Reward {RewardKey} granted to hero {HeroId} for {RewardSourceType} {CauseId}",
            rewardKey,
            request.HeroId,
            request.SourceType,
            request.CauseId
        );

        return ToResult(reward, alreadyGranted: false);
    }

    private GrantRewardResult AcknowledgeDuplicate(
        RewardEntity existing,
        GrantRewardCommand request
    )
    {
        if (existing.Status != RewardStatus.Applied)
        {
            throw new InvalidOperationException(
                $"Reward '{existing.RewardKey}' exists but is not applied."
            );
        }

        if (!HasSameContent(existing, request))
        {
            throw new InvalidOperationException(
                $"Reward '{existing.RewardKey}' was already granted with a different content."
            );
        }

        logger.Information(
            "Duplicate reward operation {RewardKey} ignored for hero {HeroId}",
            existing.RewardKey,
            request.HeroId
        );

        return ToResult(existing, alreadyGranted: true);
    }

    private static bool HasSameContent(RewardEntity reward, GrantRewardCommand request) =>
        reward.RunId == request.RunId
        && reward
            .Items.Select(item => (item.ItemId, item.Quantity))
            .Order()
            .SequenceEqual(request.Items.Select(item => (item.ItemId, item.Quantity)).Order());

    private static GrantRewardResult ToResult(RewardEntity reward, bool alreadyGranted) =>
        new(
            reward.Id,
            reward
                .Items.Select(item => new GrantedRewardItem(
                    item.ItemId,
                    item.ItemInstanceId,
                    item.Quantity
                ))
                .ToList(),
            alreadyGranted
        );
}
