using FluentAssertions;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reward.Contracts.V1;
using Reward.Domain.Entities;
using Reward.Infrastructure.Persistence;
using RewardEntity = Reward.Domain.Entities.Reward;

namespace Reward.Test.Integration.EnemyLoot.Grpc;

/// <summary>Provides isolated database state and shared helpers for enemy loot gRPC tests.</summary>
[Collection(EnemyLootGrpcCollection.Name)]
public abstract class EnemyLootGrpcTestBase(EnemyLootGrpcFixture fixture) : IAsyncLifetime
{
    protected EnemyLootGrpcFixture Fixture { get; } = fixture;

    /// <summary>Resets persisted test data before each integration test.</summary>
    public ValueTask InitializeAsync() => new(Fixture.ResetAsync());

    /// <summary>Completes test cleanup without asynchronous resources.</summary>
    public ValueTask DisposeAsync()
    {
        // A derived test class may add a finalizer later, so the base class suppresses it.
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    protected Task<EnemyLootReply> GrantAsync(GrantEnemyLootRequest request) =>
        Fixture
            .CreateClient()
            .GrantEnemyLootAsync(request, cancellationToken: TestContext.Current.CancellationToken)
            .ResponseAsync;

    protected static async Task ShouldFailWithAsync(Func<Task> action, StatusCode statusCode) =>
        await action
            .Should()
            .ThrowAsync<RpcException>()
            .Where(exception => exception.StatusCode == statusCode);

    // Builds a monster request; a boss test sets EnemyKind afterwards.
    protected static GrantEnemyLootRequest CreateRequest(
        Guid runId,
        Guid enemyId,
        Guid lootTableId,
        CombatResult result,
        params SeededHero[] participants
    )
    {
        var request = new GrantEnemyLootRequest
        {
            RunId = runId.ToString(),
            EnemyId = enemyId.ToString(),
            EnemyKind = EnemyKind.Monster,
            LootTableId = lootTableId.ToString(),
            CombatResult = result,
        };
        request.Participants.AddRange(
            participants.Select(participant => new EnemyLootParticipant
            {
                PlayerId = participant.PlayerId.ToString(),
                HeroId = participant.HeroId.ToString(),
                Status = participant.Status,
            })
        );
        return request;
    }

    protected static Item CreateItem(
        string name,
        Category category,
        Rarity rarity,
        DateTimeOffset now
    ) =>
        new()
        {
            Id = Guid.NewGuid(),
            Category = category,
            Rarity = rarity,
            Name = name,
            Description = "Enemy loot.",
            LevelRequired = 1,
            CreatedAt = now,
            UpdatedAt = now,
        };

    protected static RewardSource CreateSource(string name) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = name,
        };

    protected async Task<IReadOnlyList<Guid>> GetInventoryItemIdsAsync(Guid heroId)
    {
        await using AsyncServiceScope scope = Fixture.Services.CreateAsyncScope();
        RewardDbContext dbContext = scope.ServiceProvider.GetRequiredService<RewardDbContext>();
        return await dbContext
            .ItemInstances.Where(itemInstance => itemInstance.Inventory.HeroId == heroId)
            .OrderBy(itemInstance => itemInstance.Id)
            .Select(itemInstance => itemInstance.ItemId)
            .ToListAsync(TestContext.Current.CancellationToken);
    }

    protected async Task<IReadOnlyList<RewardEntity>> GetRewardsAsync()
    {
        await using AsyncServiceScope scope = Fixture.Services.CreateAsyncScope();
        RewardDbContext dbContext = scope.ServiceProvider.GetRequiredService<RewardDbContext>();
        return await dbContext
            .Rewards.Include(reward => reward.Items)
            .ToListAsync(TestContext.Current.CancellationToken);
    }

    protected sealed record SeededHero(Guid PlayerId, Guid HeroId, ParticipantStatus Status);
}
