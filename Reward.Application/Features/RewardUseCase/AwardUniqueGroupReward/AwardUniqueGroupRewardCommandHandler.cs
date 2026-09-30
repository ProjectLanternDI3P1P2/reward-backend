using MediatR;
using Reward.Domain.Entities;
using Reward.Domain.Enums;
using Reward.Domain.Repositories;
using Reward.Domain.Services;
using Reward.Domain.ValueObjects;
using ILogger = Serilog.ILogger;
using RewardEntity = Reward.Domain.Entities.Reward;

namespace Reward.Application.Features.RewardUseCase.AwardUniqueGroupReward;

public sealed class AwardUniqueGroupRewardCommandHandler(
    IRewardRepository rewardRepository,
    IInventoryRepository inventoryRepository,
    IRandomSource random,
    IClock clock,
    ILogger logger
) : IRequestHandler<AwardUniqueGroupRewardCommand, AwardUniqueGroupRewardResult>
{
    public async Task<AwardUniqueGroupRewardResult> Handle(
        AwardUniqueGroupRewardCommand request,
        CancellationToken cancellationToken
    )
    {
        string rewardKey = RewardEntity.CreateUniqueKey(
            request.SourceType,
            request.CauseId,
            request.ItemId
        );

        RewardEntity? existing = await rewardRepository.GetByKeyAsync(rewardKey, cancellationToken);
        if (existing is not null)
        {
            return AcknowledgeDuplicate(existing, request);
        }

        // Concurrent deliveries may draw different recipients and would lock different
        // inventories, so they are serialized on the reward key itself.
        await rewardRepository.LockKeyAsync(rewardKey, cancellationToken);
        existing = await rewardRepository.GetByKeyAsync(rewardKey, cancellationToken);
        if (existing is not null)
        {
            return AcknowledgeDuplicate(existing, request);
        }

        ActivityParticipant recipient = UniqueRewardRecipientSelector.SelectRecipient(
            request.Participants,
            random
        );

        Inventory inventory =
            await inventoryRepository.GetByHeroIdForUpdateAsync(recipient.HeroId, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"Inventory for hero '{recipient.HeroId}' was not found."
            );

        string sourceName = request.SourceType.ToCode();
        RewardSource source =
            await rewardRepository.GetSourceByNameAsync(sourceName, cancellationToken)
            ?? throw new InvalidOperationException(
                $"The reward source '{sourceName}' is not configured."
            );

        Item item =
            await inventoryRepository.GetItemByIdAsync(request.ItemId, cancellationToken)
            ?? throw new KeyNotFoundException($"Item '{request.ItemId}' was not found.");

        DateTimeOffset now = clock.UtcNow;
        var reward = new RewardEntity
        {
            Id = Guid.NewGuid(),
            HeroId = recipient.HeroId,
            RunId = request.RunId,
            RewardSourceId = source.Id,
            RewardSource = source,
            Type = RewardType.Item,
            Status = RewardStatus.Pending,
            RewardKey = rewardKey,
            CreatedAt = now,
        };
        ItemInstance itemInstance = reward.CreditItem(inventory, item, request.Quantity, now);
        inventoryRepository.AddItemInstance(itemInstance);

        // Committed with the item in the same transaction: a persisted reward is fully applied.
        reward.Status = RewardStatus.Applied;
        rewardRepository.Add(reward);

        logger.Information(
            "Unique reward {RewardKey} awarded to hero {HeroId} of player {PlayerId} for {RewardSourceType} {CauseId}",
            rewardKey,
            recipient.HeroId,
            recipient.PlayerId,
            request.SourceType,
            request.CauseId
        );

        return ToResult(reward, alreadyAwarded: false);
    }

    private AwardUniqueGroupRewardResult AcknowledgeDuplicate(
        RewardEntity existing,
        AwardUniqueGroupRewardCommand request
    )
    {
        if (existing.Status != RewardStatus.Applied)
        {
            throw new InvalidOperationException(
                $"Reward '{existing.RewardKey}' exists but is not applied."
            );
        }

        RewardItem awardedItem = existing.Items.Single();
        if (existing.RunId != request.RunId || awardedItem.Quantity != request.Quantity)
        {
            throw new InvalidOperationException(
                $"Reward '{existing.RewardKey}' was already awarded with a different content."
            );
        }

        logger.Information(
            "Duplicate unique reward operation {RewardKey} ignored; already awarded to hero {HeroId}",
            existing.RewardKey,
            existing.HeroId
        );

        return ToResult(existing, alreadyAwarded: true);
    }

    private static AwardUniqueGroupRewardResult ToResult(RewardEntity reward, bool alreadyAwarded)
    {
        RewardItem awardedItem = reward.Items.Single();
        return new AwardUniqueGroupRewardResult(
            reward.Id,
            reward.HeroId,
            awardedItem.ItemId,
            awardedItem.ItemInstanceId,
            awardedItem.Quantity,
            alreadyAwarded
        );
    }
}
