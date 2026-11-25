namespace Reward.Infrastructure.Grpc.Configuration;

public sealed class PlayerGrpcClientOptions
{
    public const string SectionName = "Grpc:Player";

    /// <summary>Internal gRPC endpoint of the Player service, set by configuration.</summary>
    public string Address { get; init; } = string.Empty;

    public int TimeoutSeconds { get; init; } = 2;

    /// <summary>
    /// Local development only: answer every participation question with "participant",
    /// until Player implements RewardPlayerService. Never enable outside a developer machine.
    /// </summary>
    public bool UseDevelopmentMock { get; init; }
}
