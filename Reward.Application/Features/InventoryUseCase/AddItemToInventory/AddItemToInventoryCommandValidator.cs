using FluentValidation;

namespace Reward.Application.Features.InventoryUseCase.AddItemToInventory;

public sealed class AddItemToInventoryCommandValidator
    : AbstractValidator<AddItemToInventoryCommand>
{
    public AddItemToInventoryCommandValidator()
    {
        RuleFor(command => command.HeroId).NotEmpty();
        RuleFor(command => command.ItemId).NotEmpty();
        RuleFor(command => command.IdempotencyKey).NotEmpty().MaximumLength(200);
    }
}
