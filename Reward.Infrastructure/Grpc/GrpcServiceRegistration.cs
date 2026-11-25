using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Reward.Application.Ports;
using Reward.Contracts.V1;
using Reward.Infrastructure.Grpc.Clients;
using Reward.Infrastructure.Grpc.Configuration;
using Reward.Infrastructure.Services;

namespace Reward.Infrastructure.Grpc;

public static class GrpcServiceRegistration
{
    public static IServiceCollection AddGrpcConfiguration(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var options =
            configuration
                .GetSection(PlaceholderGrpcClientOptions.SectionName)
                .Get<PlaceholderGrpcClientOptions>()
            ?? new PlaceholderGrpcClientOptions();

        if (!Uri.TryCreate(options.Address, UriKind.Absolute, out _))
        {
            throw new InvalidOperationException(
                "Grpc:Placeholder:Address must be an absolute URI."
            );
        }

        services.AddPlayerParticipation(configuration);

        return services
            .AddSingleton(Options.Create(options))
            .AddGrpcClient<RewardPlaceholderService.RewardPlaceholderServiceClient>(client =>
                client.Address = new Uri(options.Address)
            )
            .Services.AddScoped<IPlaceholderClient, PlaceholderGrpcClient>();
    }

    private static void AddPlayerParticipation(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        IConfigurationSection section = configuration.GetSection(
            PlayerGrpcClientOptions.SectionName
        );
        if (section.GetValue<bool>(nameof(PlayerGrpcClientOptions.UseDevelopmentMock)))
        {
            services.AddScoped<IPlayerParticipationClient, DevelopmentPlayerParticipationClient>();
            return;
        }

        // Validated when the API starts, not at registration: the migrate-only command builds
        // the host without starting it and must not need the Player address.
        services
            .AddOptions<PlayerGrpcClientOptions>()
            .Bind(section)
            .Validate(
                options => Uri.TryCreate(options.Address, UriKind.Absolute, out _),
                "Grpc:Player:Address must be an absolute URI."
            )
            .Validate(
                options => options.TimeoutSeconds > 0,
                "Grpc:Player:TimeoutSeconds must be greater than zero."
            )
            .ValidateOnStart();

        services
            .AddGrpcClient<RewardPlayerService.RewardPlayerServiceClient>(
                (provider, client) =>
                    client.Address = new Uri(
                        provider
                            .GetRequiredService<IOptions<PlayerGrpcClientOptions>>()
                            .Value.Address
                    )
            )
            .Services.AddScoped<IPlayerParticipationClient, PlayerParticipationGrpcClient>();
    }
}
