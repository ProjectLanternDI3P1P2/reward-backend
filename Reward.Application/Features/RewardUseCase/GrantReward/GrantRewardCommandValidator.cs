using FluentValidation;

namespace Reward.Application.Features.RewardUseCase.GrantReward;

public sealed class GrantRewardCommandValidator : AbstractValidator<GrantRewardCommand>
{
    public GrantRewardCommandValidator()
    {
        RuleFor(command => command.HeroId).NotEmpty();
        RuleFor(command => command.RunId).NotEmpty();
        RuleFor(command => command.SourceType).IsInEnum();
        RuleFor(command => command.CauseId).NotEmpty();
        RuleFor(command => command.Items).NotEmpty();
        RuleForEach(command => command.Items)
            .ChildRules(item =>
            {
                item.RuleFor(line => line.ItemId).NotEmpty();
                item.RuleFor(line => line.Quantity).GreaterThan(0);
            });
    }
}
