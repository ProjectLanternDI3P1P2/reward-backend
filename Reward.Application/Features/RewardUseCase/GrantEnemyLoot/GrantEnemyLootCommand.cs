using Reward.Application.Abstractions;
using Reward.Domain.Enums;
using Reward.Domain.ValueObjects;

namespace Reward.Application.Features.RewardUseCase.GrantEnemyLoot;

/// <summary>
/// Grants the loot of a defeated enemy to every eligible participant of the combat.
/// The loot is drawn from the loot table configured for that enemy, and only when the
/// combat was won.
/// </summary>
public sealed record GrantEnemyLootCommand(
    Guid RunId,
    Guid EnemyId,
    RewardSourceType EnemyType,
    Guid LootTableId,
    CombatOutcome Outcome,
    IReadOnlyList<ActivityParticipant> Participants
) : ICommand<GrantEnemyLootResult>;
