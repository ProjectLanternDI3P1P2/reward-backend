using FluentAssertions;
using FluentValidation.TestHelper;
using Reward.Application.Features.RewardUseCase.GenerateChestLoot;

namespace Reward.Test.Features.RewardUseCase;

public sealed class ChestLootIdentityTests
{
    [Fact]
    public void Create_SameContext_ReturnsStableVersion5Uuid()
    {
        // Arrange a fixed context with an externally reproducible UUIDv5 result.
        var runId = Guid.Parse("11111111-2222-3333-4444-555555555555");

        // Act twice to prove the identity does not depend on process state.
        Guid first = ChestLootIdentity.Create(runId, floor: 1, chestElementId: 0);
        Guid second = ChestLootIdentity.Create(runId, floor: 1, chestElementId: 0);

        // Assert the exact RFC UUIDv5 value and stable replay identity.
        first.Should().Be(Guid.Parse("c24d1caf-1a51-59b6-9f4d-d51bfafaa1e9"));
        second.Should().Be(first);
        first.Version.Should().Be(5);
    }

    [Fact]
    public void Create_DifferentContextParts_ReturnsIndependentIds()
    {
        // Arrange one run and a second independent run.
        Guid firstRun = Guid.NewGuid();
        Guid secondRun = Guid.NewGuid();

        // Act across each component of the business identity.
        Guid baseline = ChestLootIdentity.Create(firstRun, floor: 1, chestElementId: 0);
        Guid otherRun = ChestLootIdentity.Create(secondRun, floor: 1, chestElementId: 0);
        Guid otherFloor = ChestLootIdentity.Create(firstRun, floor: 2, chestElementId: 0);
        Guid otherElement = ChestLootIdentity.Create(firstRun, floor: 1, chestElementId: 1);

        // Assert no context component aliases another chest.
        new[] { baseline, otherRun, otherFloor, otherElement }
            .Should()
            .OnlyHaveUniqueItems();
    }

    [Theory]
    [InlineData("NORMAL")]
    [InlineData("HARD")]
    public void Validate_SupportedDifficulty_AcceptsCommand(string difficulty)
    {
        var validator = new GenerateChestLootCommandValidator();
        var command = new GenerateChestLootCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            difficulty
        );

        validator.TestValidate(command).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("ELITE")]
    [InlineData("normal")]
    [InlineData("")]
    public void Validate_UnsupportedDifficulty_RejectsCommand(string difficulty)
    {
        var validator = new GenerateChestLootCommandValidator();
        var command = new GenerateChestLootCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            difficulty
        );

        validator.TestValidate(command).ShouldHaveValidationErrorFor(value => value.Difficulty);
    }

    [Fact]
    public void Validate_NegativeHttpElementId_RejectsCommand()
    {
        var validator = new GenerateChestLootCommandValidator();
        var command = new GenerateChestLootCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            "NORMAL",
            ChestElementId: -1
        );

        validator.TestValidate(command).ShouldHaveValidationErrorFor(value => value.ChestElementId);
    }
}
