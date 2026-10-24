using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Reward.Application.Features.RewardUseCase.AwardUniqueGroupReward;
using Reward.Application.Features.RewardUseCase.GrantReward;
using Reward.Domain.Entities;
using Reward.Domain.Enums;
using Reward.Domain.ValueObjects;
using Reward.Infrastructure.Persistence;
using InventoryEntity = Reward.Domain.Entities.Inventory;
using RewardEntity = Reward.Domain.Entities.Reward;

namespace Reward.Test.Integration.Rewards;

public sealed class ViewGroupRewardDistributionIntegrationTests(RewardGrantFixture fixture)
    : RewardGrantTestBase(fixture)
{
    private readonly Guid runId = Guid.NewGuid();
    private readonly Guid bossId = Guid.NewGuid();
    private readonly ActivityParticipant first = CreateParticipant(ParticipationStatus.Active);
    private readonly ActivityParticipant second = CreateParticipant(ParticipationStatus.Dead);
    private readonly ActivityParticipant left = CreateParticipant(ParticipationStatus.Left);

    [Fact]
    public async Task Get_AfterADistribution_ListsUniqueAndSharedRewardsWithTheirRecipients()
    {
        // Arrange
        Catalogue catalogue = await SeedAsync();
        await DistributeAsync(runId, bossId, catalogue);

        // Act
        HttpResponseMessage response = await GetAsync(runId);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        DistributionResponse body = await ReadAsync(response);
        body.DungeonRunId.Should().Be(runId);
        CauseResponse cause = body.Distributions.Should().ContainSingle().Subject;
        cause.SourceType.Should().Be("BOSS");
        cause.CauseId.Should().Be(bossId);
        cause.Rewards.Should().HaveCount(2);

        RewardResponse unique = cause.Rewards.Single(reward => reward.Kind == "UNIQUE");
        unique.ItemId.Should().Be(catalogue.SwordId);
        unique.Name.Should().Be("Blazing Aegis");
        unique.Rarity.Should().Be("Legendary");
        unique.Quantity.Should().Be(1);
        RecipientResponse winner = unique.Recipients.Should().ContainSingle().Subject;
        new[] { first, second }
            .Should()
            .ContainSingle(participant =>
                participant.HeroId == winner.HeroId && participant.PlayerId == winner.PlayerId
            );
        winner.ItemInstanceId.Should().NotBeNull();

        RewardResponse shared = cause.Rewards.Single(reward =>
            reward.Kind == "SHARED_PER_PARTICIPANT"
        );
        shared.ItemId.Should().Be(catalogue.PotionId);
        shared.Quantity.Should().Be(2);
        shared
            .Recipients.Select(recipient => (recipient.HeroId, recipient.PlayerId))
            .Should()
            .BeEquivalentTo([
                (first.HeroId, (Guid?)first.PlayerId),
                (second.HeroId, (Guid?)second.PlayerId),
            ]);
    }

    [Fact]
    public async Task Get_RunWithoutGroupRewards_ReturnsAnEmptyDistribution()
    {
        // Arrange
        await SeedAsync();

        // Act
        HttpResponseMessage response = await GetAsync(Guid.NewGuid());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadAsync(response)).Distributions.Should().BeEmpty();
    }

    [Fact]
    public async Task Get_RunWithChestAndOtherRunRewards_ListsOnlyItsOwnGroupRewards()
    {
        // Arrange
        Catalogue catalogue = await SeedAsync();
        await DistributeAsync(runId, bossId, catalogue);
        await DistributeAsync(Guid.NewGuid(), Guid.NewGuid(), catalogue);
        await AddChestRewardAsync(runId, first.HeroId, catalogue);

        // Act
        DistributionResponse body = await ReadAsync(await GetAsync(runId));

        // Assert
        CauseResponse cause = body.Distributions.Should().ContainSingle().Subject;
        cause.CauseId.Should().Be(bossId);
        cause.Rewards.Should().HaveCount(2);
    }

    [Fact]
    public async Task Get_EmptyRunId_ReturnsValidationError()
    {
        // Act
        HttpResponseMessage response = await GetAsync(Guid.Empty);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Get_MalformedRunId_ReturnsNotFound()
    {
        // Act
        HttpResponseMessage response = await Fixture.HttpClient.GetAsync(
            "/api/v1/runs/not-a-guid/rewards",
            TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task DistributeAsync(Guid distributedRunId, Guid causeId, Catalogue catalogue)
    {
        await using AsyncServiceScope scope = Fixture.Services.CreateAsyncScope();
        ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();

        // One unique reward among the eligible participants...
        await sender.Send<AwardUniqueGroupRewardResult>(
            new AwardUniqueGroupRewardCommand(
                distributedRunId,
                RewardSourceType.Boss,
                causeId,
                catalogue.SwordId,
                1,
                [first, second, left]
            ),
            TestContext.Current.CancellationToken
        );

        // ...and the same reward credited to every eligible participant.
        foreach (ActivityParticipant participant in new[] { first, second })
        {
            await sender.Send<GrantRewardResult>(
                new GrantRewardCommand(
                    participant.HeroId,
                    distributedRunId,
                    RewardSourceType.Boss,
                    causeId,
                    [new GrantRewardItem(catalogue.PotionId, 2)],
                    participant.PlayerId
                ),
                TestContext.Current.CancellationToken
            );
        }
    }

    private async Task<HttpResponseMessage> GetAsync(Guid requestedRunId) =>
        await Fixture.HttpClient.GetAsync(
            $"/api/v1/runs/{requestedRunId}/rewards",
            TestContext.Current.CancellationToken
        );

    private static async Task<DistributionResponse> ReadAsync(HttpResponseMessage response) =>
        (
            await response.Content.ReadFromJsonAsync<DistributionResponse>(
                TestContext.Current.CancellationToken
            )
        )!;

    private static ActivityParticipant CreateParticipant(ParticipationStatus status) =>
        new(Guid.NewGuid(), Guid.NewGuid(), status);

    private async Task<Catalogue> SeedAsync()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var weapons = new Category
        {
            Id = Guid.NewGuid(),
            Label = "WEAPON",
            CreatedAt = now,
            UpdatedAt = now,
        };
        var potions = new Category
        {
            Id = Guid.NewGuid(),
            Label = "POTION",
            CreatedAt = now,
            UpdatedAt = now,
        };
        var legendary = new Rarity
        {
            Id = Guid.NewGuid(),
            Label = "Legendary",
            Color = "#F59E0B",
            Rank = 5,
            CreatedAt = now,
            UpdatedAt = now,
        };
        Item sword = CreateItem("Blazing Aegis", weapons, legendary, stackable: false, now);
        Item potion = CreateItem("Health Potion", potions, legendary, stackable: true, now);

        await using AsyncServiceScope scope = Fixture.Services.CreateAsyncScope();
        RewardDbContext dbContext = scope.ServiceProvider.GetRequiredService<RewardDbContext>();
        dbContext.AddRange(weapons, potions, legendary, sword, potion);
        dbContext.AddRange(
            new RewardSource
            {
                Id = Guid.NewGuid(),
                Name = "BOSS",
                Description = "Boss.",
            },
            new RewardSource
            {
                Id = Guid.NewGuid(),
                Name = "CHEST",
                Description = "Chest.",
            }
        );
        dbContext.AddRange(
            new[] { first, second, left }.Select(participant => new InventoryEntity
            {
                Id = Guid.NewGuid(),
                HeroId = participant.HeroId,
                ItemCapacity = 40,
                PotionCapacity = 20,
                CreatedAt = now,
                UpdatedAt = now,
            })
        );
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        return new Catalogue(sword.Id, potion.Id);
    }

    // Simulates a chest claimed by a single hero: not a group distribution.
    private async Task AddChestRewardAsync(Guid chestRunId, Guid heroId, Catalogue catalogue)
    {
        await using AsyncServiceScope scope = Fixture.Services.CreateAsyncScope();
        RewardDbContext dbContext = scope.ServiceProvider.GetRequiredService<RewardDbContext>();
        RewardSource chest = dbContext.RewardSources.Single(source => source.Name == "CHEST");
        dbContext.Rewards.Add(
            new RewardEntity
            {
                Id = Guid.NewGuid(),
                HeroId = heroId,
                RunId = chestRunId,
                RewardSourceId = chest.Id,
                Type = RewardType.Item,
                Status = RewardStatus.Applied,
                RewardKey = $"chest:{chestRunId:N}:{Guid.NewGuid():N}",
                CreatedAt = DateTimeOffset.UtcNow,
                Items =
                [
                    new RewardItem
                    {
                        Id = Guid.NewGuid(),
                        ItemId = catalogue.SwordId,
                        ItemNameSnapshot = "Blazing Aegis",
                        ItemRaritySnapshot = "Legendary",
                        Quantity = 1,
                        CreatedAt = DateTimeOffset.UtcNow,
                    },
                ],
            }
        );
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static Item CreateItem(
        string name,
        Category category,
        Rarity rarity,
        bool stackable,
        DateTimeOffset now
    ) =>
        new()
        {
            Id = Guid.NewGuid(),
            Category = category,
            Rarity = rarity,
            Name = name,
            Description = name,
            LevelRequired = 1,
            Stackable = stackable,
            CreatedAt = now,
            UpdatedAt = now,
        };

    private sealed record Catalogue(Guid SwordId, Guid PotionId);

    private sealed record DistributionResponse(
        Guid DungeonRunId,
        List<CauseResponse> Distributions
    );

    private sealed record CauseResponse(
        string SourceType,
        Guid CauseId,
        List<RewardResponse> Rewards
    );

    private sealed record RewardResponse(
        string Kind,
        Guid ItemId,
        string Name,
        string Rarity,
        int Quantity,
        List<RecipientResponse> Recipients
    );

    private sealed record RecipientResponse(Guid HeroId, Guid? PlayerId, Guid? ItemInstanceId);
}
