using Grpc.Core;
using Reward.Contracts.V1;
using Reward.Domain.Repositories;

namespace Reward.Presentation.Grpc.Services;

public sealed class PlaceholderGrpcService(IPlaceholderRepository repository) : RewardPlaceholderService.RewardPlaceholderServiceBase
{
    public override async Task<PlaceholderReply> GetPlaceholder(GetPlaceholderRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.PlaceholderId, out var placeholderId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "placeholder_id must be a GUID."));
        }

        var placeholder = await repository.GetByIdAsync(placeholderId, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "Placeholder not found."));

        return new PlaceholderReply { Id = placeholder.Id.ToString(), Name = placeholder.Name };
    }
}
