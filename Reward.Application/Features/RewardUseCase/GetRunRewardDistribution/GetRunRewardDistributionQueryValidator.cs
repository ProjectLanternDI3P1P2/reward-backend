using FluentValidation;

namespace Reward.Application.Features.RewardUseCase.GetRunRewardDistribution;

public sealed class GetRunRewardDistributionQueryValidator
    : AbstractValidator<GetRunRewardDistributionQuery>
{
    public GetRunRewardDistributionQueryValidator()
    {
        RuleFor(query => query.DungeonRunId).NotEmpty();
    }
}
