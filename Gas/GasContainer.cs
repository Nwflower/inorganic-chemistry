using System;

namespace ElementalIndustry;

/// <summary>
/// A gas stack's unit count is its mass in grams. The gas type is kept on the
/// container, so the normal inventory can split or merge identical gases.
/// </summary>
internal static class GasContainer
{
    public const string GasContainerTag = "GAS_CONTAINER_TAG";
    public const string GasTypeTag = "GAS_TYPE";

    public static void Init(GameItem item, string gasType)
    {
        // The valve is narrower than the tank: reserve the complete 2x3 area
        // instead of deriving an irregular shape from sampled sprite pixels.
        item.SetShape(new GridShapeBuilder(2, 3).SetData(new byte[] { 1, 1, 1, 1, 1, 1 }, 2));
        item.EnableTag(GasContainerTag, skipValidate: true);
        // Reads use modifiedState in the game; publish both tags before using them.
        item.ModifyTag(GasTypeTag, state => state.Enable().SetString(gasType ?? ""));
        // Shop/loot cylinders start with 100 g; machine outputs set their own mass.
        item.SetUnitCount(100);
        item.onChangeAmountItemFunc += ProductQuality.RefreshValue;
        ProductQuality.RefreshValue(item);
    }

    /// <summary>
    /// Equal gases stack into one slot and a slot never fills up; this replaces the old
    /// gas filler machine, which only existed to split and recombine fixed-size bottles.
    /// </summary>
    public static void MakeStackable(GameItem item)
    {
        item.mayStackItemFunc = (self, other) =>
            other != null &&
            IsGasContainer(other) &&
            string.Equals(GetGasType(self), GetGasType(other), StringComparison.OrdinalIgnoreCase);
        item.maxSlotAmountItemFunc = (_, _, _) => int.MaxValue;
    }

    public static bool IsGasContainer(GameItem? item)
    {
        return item != null && item.IsTag(GasContainerTag);
    }

    public static bool Is(GameItem? item, string gasType)
    {
        return IsGasContainer(item) &&
            string.Equals(GetGasType(item), gasType, StringComparison.OrdinalIgnoreCase);
    }

    public static string GetGasType(GameItem? item)
    {
        if (!IsGasContainer(item))
        {
            return "";
        }
        string type = item!.GetTagReadonly(GasTypeTag).GetString();
        return string.IsNullOrEmpty(type) ? "" : type;
    }

    /// <summary>Consumes grams from a stack, leaving the rest intact.</summary>
    public static void Consume(GameItem item, int count = 1)
    {
        if (item == null || count <= 0)
        {
            return;
        }
        int remaining = item.unitCount - count;
        if (remaining > 0)
        {
            item.SetUnitCount(remaining);
            ProductQuality.RefreshValue(item);
        }
        else
        {
            item.Destroy();
        }
    }

    public static string GetDisplayGas(string gasType)
    {
        if (string.IsNullOrEmpty(gasType))
        {
            return Loc.Get("gas.empty");
        }
        return Loc.Get("gas." + gasType.ToLowerInvariant());
    }
}
