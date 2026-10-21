using FluentValidation;
using Reward.Domain.Enums;

namespace Reward.Application.Features.RewardUseCase.GrantEnemyLoot;

public sealed class GrantEnemyLootCommandValidator : AbstractValidator<GrantEnemyLootCommand>
{
    public GrantEnemyLootCommandValidator()
    {
        RuleFor(command => command.RunId).NotEmpty();
        RuleFor(command => command.EnemyId).NotEmpty();
        RuleFor(command => command.EnemyType)
            .Must(enemyType => enemyType is RewardSourceType.Monster or RewardSourceType.Boss)
            .WithMessage("An enemy must be a monster or a boss.");
        RuleFor(command => command.LootTableId).NotEmpty();
        RuleFor(command => command.Outcome).IsInEnum();
        RuleFor(command => command.Participants).NotEmpty();
        RuleForEach(command => command.Participants)
            .ChildRules(participant =>
            {
                participant.RuleFor(value => value.PlayerId).NotEmpty();
                participant.RuleFor(value => value.HeroId).NotEmpty();
                participant.RuleFor(value => value.Status).IsInEnum();
            });
        RuleFor(command => command.Participants)
            .Must(participants =>
                participants.Select(participant => participant.PlayerId).Distinct().Count()
                == participants.Count
            )
            .When(command => command.Participants is not null)
            .WithMessage("A player cannot be registered more than once.");
        RuleFor(command => command.Participants)
            .Must(participants =>
                participants.Select(participant => participant.HeroId).Distinct().Count()
                == participants.Count
            )
            .When(command => command.Participants is not null)
            .WithMessage("A hero cannot be registered more than once.");
    }
}
