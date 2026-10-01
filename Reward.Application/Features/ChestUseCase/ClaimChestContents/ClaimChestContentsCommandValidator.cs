using FluentValidation;

namespace Reward.Application.Features.ChestUseCase.ClaimChestContents;

public sealed class ClaimChestContentsCommandValidator
    : AbstractValidator<ClaimChestContentsCommand>
{
    public ClaimChestContentsCommandValidator()
    {
        RuleFor(command => command.HeroId).NotEmpty();
        RuleFor(command => command.DungeonRunId).NotEmpty();
        RuleFor(command => command.ChestId).NotEmpty();
    }
}
