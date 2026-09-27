using System.Text.Json.Serialization;

namespace Reward.Presentation.DTO;

public sealed record EquipItemRequest([property: JsonRequired] Guid SlotId);
