# Reward.Contracts

Versioned Protocol Buffers contract and generated C# gRPC client/server types for
the Reward service.

## Installation

Configure the organisation's GitHub Packages NuGet source, then pin a released
version of the package:

```xml
<PackageReference Include="Reward.Contracts" Version="0.1.1" />
```

For a C# gRPC consumer, also reference `Grpc.Net.Client`, create a channel for the
Reward service's internal endpoint, then construct
`Reward.Contracts.V1.RewardInventoryService.RewardInventoryServiceClient`.

The original `.proto` source is included in this package under `proto/`.
