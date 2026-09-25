using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Reward.Contracts.V1;
using Reward.Infrastructure.Grpc.Clients;
using Reward.Infrastructure.Grpc.Configuration;

namespace Reward.Infrastructure.Grpc;

public static class GrpcServiceRegistration
{
    public static IServiceCollection AddGrpcConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(PlaceholderGrpcClientOptions.SectionName).Get<PlaceholderGrpcClientOptions>()
            ?? new PlaceholderGrpcClientOptions();

        if (!Uri.TryCreate(options.Address, UriKind.Absolute, out _))
        {
            throw new InvalidOperationException("Grpc:Placeholder:Address must be an absolute URI.");
        }

        return services
            .AddSingleton(Options.Create(options))
            .AddGrpcClient<RewardPlaceholderService.RewardPlaceholderServiceClient>(client => client.Address = new Uri(options.Address))
            .Services
            .AddScoped<Reward.Application.Ports.IPlaceholderClient, PlaceholderGrpcClient>();
    }
}
