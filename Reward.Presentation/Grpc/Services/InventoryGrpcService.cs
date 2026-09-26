using Grpc.Core;
using MediatR;
using Reward.Application.Features.InventoryUseCase.GetActiveEquipment;
using Reward.Application.Features.InventoryUseCase.GetHeroInventory;
using Reward.Contracts.V1;

namespace Reward.Presentation.Grpc.Services;

/// <summary>Exposes the versioned inventory read contract to other backend services.</summary>
public sealed class InventoryGrpcService(ISender sender) : RewardInventoryService.RewardInventoryServiceBase
{
    public override async Task<HeroInventoryReply> GetHeroInventory(GetHeroInventoryRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.HeroId, out Guid heroId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "hero_id must be a GUID."));
        }

        HeroInventory inventory = await sender.Send(new GetHeroInventoryQuery(heroId), context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "Hero inventory not found."));

        var reply = new HeroInventoryReply { HeroId = inventory.HeroId.ToString() };
        reply.Items.AddRange(inventory.Items.Select(MapItem));
        reply.Consumables.AddRange(inventory.Consumables.Select(MapItem));
        return reply;
    }

    public override async Task<ActiveEquipmentReply> GetActiveEquipment(
        GetActiveEquipmentRequest request,
        ServerCallContext context)
    {
        if (!Guid.TryParse(request.HeroId, out Guid heroId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "hero_id must be a GUID."));
        }

        ActiveEquipment equipment = await sender.Send(
            new GetActiveEquipmentQuery(heroId),
            context.CancellationToken) ?? throw new RpcException(
                new Status(StatusCode.NotFound, "Hero inventory not found."));

        var reply = new ActiveEquipmentReply { HeroId = equipment.HeroId.ToString() };
        reply.Slots.AddRange(equipment.Slots.Select(slot =>
        {
            var result = new Reward.Contracts.V1.ActiveEquipmentSlot
            {
                SlotId = slot.SlotId.ToString(),
                SlotName = slot.SlotName
            };

            if (slot.EquippedItem is not null)
            {
                result.EquippedItem = MapEquippedItem(slot.EquippedItem);
            }

            return result;
        }));
        return reply;
    }

    private static Reward.Contracts.V1.InventoryItem MapItem(
        Reward.Application.Features.InventoryUseCase.GetHeroInventory.InventoryItem item) => new()
        {
            Id = item.Id.ToString(),
            Type = item.Type,
            Name = item.Name,
            Rarity = item.Rarity,
            State = item.State,
            Quantity = item.Quantity,
            IsEquipped = item.IsEquipped,
            IsReserved = item.IsReserved
        };

    private static Reward.Contracts.V1.EquippedItem MapEquippedItem(
        Reward.Application.Features.InventoryUseCase.GetActiveEquipment.EquippedItem item)
    {
        var result = new Reward.Contracts.V1.EquippedItem
        {
            ItemInstanceId = item.ItemInstanceId.ToString(),
            ItemId = item.ItemId.ToString(),
            Type = item.Type,
            Name = item.Name,
            Rarity = item.Rarity
        };
        result.Modifiers.AddRange(item.Modifiers.Select(modifier => new Reward.Contracts.V1.CombatModifier
        {
            Name = modifier.Name,
            Stat = modifier.Stat,
            Value = (double)modifier.Value,
            Type = modifier.Type
        }));
        return result;
    }
}
