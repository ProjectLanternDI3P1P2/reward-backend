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

    private static IServiceCollection AddPlayerParticipation(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        PlayerGrpcClientOptions options =
            configuration
                .GetSection(PlayerGrpcClientOptions.SectionName)
                .Get<PlayerGrpcClientOptions>()
            ?? new PlayerGrpcClientOptions();

        if (options.UseDevelopmentMock)
        {
            return services.AddScoped<
                IPlayerParticipationClient,
                DevelopmentPlayerParticipationClient
            >();
        }

        if (!Uri.TryCreate(options.Address, UriKind.Absolute, out _))
        {
            throw new InvalidOperationException("Grpc:Player:Address must be an absolute URI.");
        }

        if (options.TimeoutSeconds <= 0)
        {
            throw new InvalidOperationException(
                "Grpc:Player:TimeoutSeconds must be greater than zero."
            );
        }

        return services
            .AddSingleton(Options.Create(options))
            .AddGrpcClient<RewardPlayerService.RewardPlayerServiceClient>(client =>
                client.Address = new Uri(options.Address)
            )
            .Services.AddScoped<IPlayerParticipationClient, PlayerParticipationGrpcClient>();
    }
}
