using System.Security.Cryptography;
using System.Text;

namespace Reward.Application.Features.RewardUseCase.GenerateChestLoot;

/// <summary>Derives the stable UUID used to identify one dungeon chest interaction.</summary>
public static class ChestLootIdentity
{
    /// <summary>Creates an RFC 9562 version 5 UUID from the run and map element context.</summary>
    public static Guid Create(Guid dungeonRunId, int floor, int chestElementId)
    {
        // Hash the canonical namespace bytes and a culture-independent element name.
        Span<byte> namespaceBytes = stackalloc byte[16];
        dungeonRunId.TryWriteBytes(namespaceBytes, bigEndian: true, out _);
        byte[] nameBytes = Encoding.UTF8.GetBytes($"{floor}:{chestElementId}");
        byte[] input = new byte[namespaceBytes.Length + nameBytes.Length];
        namespaceBytes.CopyTo(input);
        nameBytes.CopyTo(input.AsSpan(namespaceBytes.Length));

        Span<byte> hash = stackalloc byte[20];
        SHA1.HashData(input, hash);
        Span<byte> uuidBytes = hash[..16];

        // Mark the digest as an RFC variant, name-based version 5 UUID.
        uuidBytes[6] = (byte)((uuidBytes[6] & 0x0F) | 0x50);
        uuidBytes[8] = (byte)((uuidBytes[8] & 0x3F) | 0x80);
        return new Guid(uuidBytes, bigEndian: true);
    }
}
