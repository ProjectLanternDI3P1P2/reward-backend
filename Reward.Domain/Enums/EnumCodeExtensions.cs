namespace Reward.Domain.Enums;

public static class EnumCodeExtensions
{
    public static string ToCode<TEnum>(this TEnum value)
        where TEnum : struct, Enum => value.ToString().ToUpperInvariant();

    public static bool IsConsumable(this ItemCategory category) =>
        category is ItemCategory.Potion or ItemCategory.Vial;
}
