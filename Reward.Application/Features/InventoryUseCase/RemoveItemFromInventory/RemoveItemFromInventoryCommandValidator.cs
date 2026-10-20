using FluentValidation;

namespace Reward.Application.Features.InventoryUseCase.RemoveItemFromInventory;

public sealed class RemoveItemFromInventoryCommandValidator
    : AbstractValidator<RemoveItemFromInventoryCommand>
{
    public RemoveItemFromInventoryCommandValidator()
    {
        RuleFor(command => command.HeroId).NotEmpty();
        RuleFor(command => command.ItemInstanceId).NotEmpty();
    }
}
