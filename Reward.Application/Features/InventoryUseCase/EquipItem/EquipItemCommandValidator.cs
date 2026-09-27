using FluentValidation;

namespace Reward.Application.Features.InventoryUseCase.EquipItem;

public sealed class EquipItemCommandValidator : AbstractValidator<EquipItemCommand>
{
    public EquipItemCommandValidator()
    {
        RuleFor(command => command.HeroId).NotEmpty();
        RuleFor(command => command.ItemInstanceId).NotEmpty();
        RuleFor(command => command.SlotId).NotEmpty();
    }
}
