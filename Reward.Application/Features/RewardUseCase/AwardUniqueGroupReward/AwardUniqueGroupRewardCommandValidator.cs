using FluentValidation;

namespace Reward.Application.Features.RewardUseCase.AwardUniqueGroupReward;

public sealed class AwardUniqueGroupRewardCommandValidator
    : AbstractValidator<AwardUniqueGroupRewardCommand>
{
    public AwardUniqueGroupRewardCommandValidator()
    {
        RuleFor(command => command.RunId).NotEmpty();
        RuleFor(command => command.SourceType).IsInEnum();
        RuleFor(command => command.CauseId).NotEmpty();
        RuleFor(command => command.ItemId).NotEmpty();
        RuleFor(command => command.Quantity).GreaterThan(0);
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
    }
}
