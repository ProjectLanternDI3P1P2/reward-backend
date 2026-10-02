using Grpc.Core;
using MediatR;
using Reward.Application.Features.RewardUseCase.GrantEnemyLoot;
using Reward.Contracts.V1;
using Reward.Domain.Enums;
using Reward.Domain.ValueObjects;

namespace Reward.Presentation.Grpc.Services;

/// <summary>Exposes the loot of a defeated enemy to the Combat service.</summary>
public sealed class EnemyLootGrpcService(ISender sender)
    : RewardEnemyLootService.RewardEnemyLootServiceBase
{
    /// <summary>Grants the loot of one defeated enemy, or replays the loot already granted.</summary>
    public override async Task<EnemyLootReply> GrantEnemyLoot(
        GrantEnemyLootRequest request,
        ServerCallContext context
    )
    {
        GrantEnemyLootCommand command = ToCommand(request);

        GrantEnemyLootResult result;
        try
        {
            result = await sender.Send<GrantEnemyLootResult>(command, context.CancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            // A full inventory or a replay with a different context is a business conflict,
            // not a server failure.
            throw new RpcException(new Status(StatusCode.FailedPrecondition, exception.Message));
        }

        return ToReply(result);
    }

    private static GrantEnemyLootCommand ToCommand(GrantEnemyLootRequest request)
    {
        // Reject malformed identifiers and unknown enum values before the command pipeline.
        var participants = new List<ActivityParticipant>();
        foreach (EnemyLootParticipant participant in request.Participants)
        {
            participants.Add(
                new ActivityParticipant(
                    ParseGuid(participant.PlayerId, "participants.player_id"),
                    ParseGuid(participant.HeroId, "participants.hero_id"),
                    ToParticipationStatus(participant.Status)
                )
            );
        }

        return new GrantEnemyLootCommand(
            ParseGuid(request.RunId, "run_id"),
            ParseGuid(request.EnemyId, "enemy_id"),
            ToEnemyType(request.EnemyKind),
            ParseGuid(request.LootTableId, "loot_table_id"),
            ToCombatOutcome(request.CombatResult),
            participants
        );
    }

    private static EnemyLootReply ToReply(GrantEnemyLootResult result)
    {
        var reply = new EnemyLootReply { EnemyId = result.EnemyId.ToString() };
        reply.HeroLoots.AddRange(
            result.HeroLoots.Select(heroLoot =>
            {
                var loot = new HeroEnemyLoot
                {
                    HeroId = heroLoot.HeroId.ToString(),
                    RewardId = heroLoot.RewardId.ToString(),
                    AlreadyGranted = heroLoot.AlreadyGranted,
                };
                loot.Items.AddRange(
                    heroLoot.Items.Select(item => new EnemyLootItem
                    {
                        ItemId = item.ItemId.ToString(),
                        ItemInstanceId = item.ItemInstanceId?.ToString() ?? string.Empty,
                        Quantity = item.Quantity,
                    })
                );
                return loot;
            })
        );
        return reply;
    }

    private static Guid ParseGuid(string value, string field) =>
        Guid.TryParse(value, out Guid guid) ? guid : throw Invalid($"{field} must be a GUID.");

    private static RewardSourceType ToEnemyType(EnemyKind kind) =>
        kind switch
        {
            EnemyKind.Monster => RewardSourceType.Monster,
            EnemyKind.Boss => RewardSourceType.Boss,
            _ => throw Invalid("enemy_kind must be monster or boss."),
        };

    private static CombatOutcome ToCombatOutcome(CombatResult result) =>
        result switch
        {
            CombatResult.Won => CombatOutcome.Won,
            CombatResult.Lost => CombatOutcome.Lost,
            _ => throw Invalid("combat_result must be won or lost."),
        };

    private static ParticipationStatus ToParticipationStatus(ParticipantStatus status) =>
        status switch
        {
            ParticipantStatus.Active => ParticipationStatus.Active,
            ParticipantStatus.Dead => ParticipationStatus.Dead,
            ParticipantStatus.Disconnected => ParticipationStatus.Disconnected,
            ParticipantStatus.Left => ParticipationStatus.Left,
            _ => throw Invalid("participants.status must be a known participant status."),
        };

    private static RpcException Invalid(string detail) =>
        new(new Status(StatusCode.InvalidArgument, detail));
}
