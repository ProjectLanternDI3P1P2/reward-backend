using FluentValidation;

namespace Reward.Application.Features.RewardUseCase.GenerateChestLoot;

/// <summary>Validates the normalized dungeon context used to locate a loot table.</summary>
public sealed class GenerateChestLootCommandValidator : AbstractValidator<GenerateChestLootCommand>
{
    public GenerateChestLootCommandValidator()
    {
        // Require distributed identifiers before taking a database lock.
        RuleFor(command => command.CommandId).NotEmpty();
        RuleFor(command => command.DungeonRunId).NotEmpty();
        RuleFor(command => command.ChestId).NotEmpty();

        // Keep the lookup context inside the supported persisted shape.
        RuleFor(command => command.Floor).GreaterThan(0);
        RuleFor(command => command.Difficulty)
            .NotEmpty()
            .MaximumLength(50)
            .Matches("^[A-Z][A-Z0-9_]*$")
            .WithMessage("difficulty must contain only uppercase letters, digits or underscores.");
    }
}
