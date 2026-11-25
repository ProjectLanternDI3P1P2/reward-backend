using Microsoft.Extensions.Options;
using AspNetCorsOptions = Microsoft.AspNetCore.Cors.Infrastructure.CorsOptions;
namespace Reward.Presentation.Configuration;

public sealed class GameClientCorsPolicyConfiguration(IOptions<GameClientCorsOptions> options)
    : IConfigureOptions<AspNetCorsOptions>
{
    public void Configure(AspNetCorsOptions corsOptions)
    {
        GameClientCorsOptions settings = options.Value;
        corsOptions.AddPolicy(
            Extensions.BuilderExtension.GameClientCorsPolicy,
            policy =>
                policy
                    .WithOrigins(settings.AllowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()
        );
    }
}
