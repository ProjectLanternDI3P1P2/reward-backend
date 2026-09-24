using Reward.Application.Abstractions;

namespace Reward.Application.Features.PlayerUseCase.CreatePlayer;

public record CreatePlayerCommand(
    Guid Id,
    string Name,
    int Attack,
    int Health,
    int MaxHealth) : ICommand;
