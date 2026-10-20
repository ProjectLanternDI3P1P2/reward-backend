using FluentValidation;

namespace Reward.Application.Features.InventoryUseCase.ConsumeConsumable;

public sealed class ConsumeConsumableCommandValidator : AbstractValidator<ConsumeConsumableCommand>
{
    public ConsumeConsumableCommandValidator()
    {
        RuleFor(command => command.HeroId).NotEmpty();
        RuleFor(command => command.ItemInstanceId).NotEmpty();
        RuleFor(command => command.IdempotencyKey).NotEmpty().MaximumLength(200);
    }
}
