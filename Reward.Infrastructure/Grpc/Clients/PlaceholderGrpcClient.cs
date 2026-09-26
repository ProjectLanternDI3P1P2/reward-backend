using Grpc.Core;
using Reward.Application.Ports;
using Reward.Contracts.V1;

namespace Reward.Infrastructure.Grpc.Clients;

public sealed class PlaceholderGrpcClient(
    RewardPlaceholderService.RewardPlaceholderServiceClient client
) : IPlaceholderClient
{
    public async Task<PlaceholderClientResult?> GetByIdAsync(
        Guid placeholderId,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var response = await client.GetPlaceholderAsync(
                new GetPlaceholderRequest { PlaceholderId = placeholderId.ToString() },
                cancellationToken: cancellationToken
            );

            return new PlaceholderClientResult(Guid.Parse(response.Id), response.Name);
        }
        catch (RpcException exception) when (exception.StatusCode == StatusCode.NotFound)
        {
            return null;
        }
    }
}
