namespace Reward.Presentation.Configuration;

public sealed class GameClientCorsOptions
{
    public const string SectionName = "Cors";

    public string[] AllowedOrigins { get; init; } = [];
}
