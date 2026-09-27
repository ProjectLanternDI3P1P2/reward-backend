using FluentValidation;

namespace Reward.Application.Features.InventoryUseCase.UnequipItem;

public sealed class UnequipItemCommandValidator : AbstractValidator<UnequipItemCommand>
{
    public UnequipItemCommandValidator()
    {
        RuleFor(command => command.HeroId).NotEmpty();
        RuleFor(command => command.ItemInstanceId).NotEmpty();
    }
}
