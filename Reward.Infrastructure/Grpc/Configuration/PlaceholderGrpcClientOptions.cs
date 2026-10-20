namespace Reward.Infrastructure.Grpc.Configuration;

public sealed class PlaceholderGrpcClientOptions
{
    public const string SectionName = "Grpc:Placeholder";
    public string Address { get; init; } = "http://localhost:8081";
    public int TimeoutSeconds { get; init; } = 2;
}
