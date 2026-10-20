using System.Text.Json.Serialization;

namespace Reward.Presentation.DTO;

public sealed record ConsumeConsumableRequest([property: JsonRequired] string IdempotencyKey);
