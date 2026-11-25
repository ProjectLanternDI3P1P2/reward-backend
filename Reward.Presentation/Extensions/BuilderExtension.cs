using Reward.Presentation.Controllers;
using Reward.Presentation.Extensions.LogExtension;
using Reward.Presentation.Grpc.Interceptors;
using Reward.Presentation.Middleware;
using Serilog;

namespace Reward.Presentation.Extensions;

public static class BuilderExtension
{
    public static WebApplicationBuilder ConfigureApi(this WebApplicationBuilder builder)
    {
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddOpenApi();
        builder.Services.AddHealthChecks();
        builder.Services.AddGrpc(options =>
        {
            options.Interceptors.Add<CorrelationIdInterceptor>();
            options.Interceptors.Add<GrpcExceptionInterceptor>();
        });

        ConfigureLogger(builder);

        builder.Services.AddTransient<ExceptionHandlingMiddleware>();
        builder.Services.AddTransient<CorrelationIdInterceptor>();
        builder.Services.AddTransient<GrpcExceptionInterceptor>();
        builder
            .Services.AddHttpClient(
                RunRewardsController.PlayerMembershipClient,
                client =>
                {
                    client.BaseAddress = new Uri(builder.Configuration["Player:BaseUrl"]!);
                    client.Timeout = TimeSpan.FromSeconds(5);
                }
            )
            .ConfigurePrimaryHttpMessageHandler(() =>
                new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false }
            );

        return builder;
    }

    private static void ConfigureLogger(WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog(
            (context, loggerConfiguration) =>
            {
                loggerConfiguration
                    .ReadFrom.Configuration(context.Configuration)
                    .Enrich.FromLogContext()
                    .Enrich.With<LowercaseLevelEnricher>()
                    .Destructure.With<IgnoreLoggingDestructuringPolicy>();
            },
            preserveStaticLogger: true
        );
    }
}
