using System;

namespace ElementalIndustry;

/// <summary>
/// A gas stack's unit count is its mass in grams. The gas type is kept on the
/// container, so the normal inventory can split or merge identical gases.
/// One item holds at most <see cref="MaxGramsPerItem"/> grams, the same way a
/// feed piece is capped at one item's worth of calories.
/// </summary>
internal static class GasContainer
{
    public const string GasContainerTag = "GAS_CONTAINER_TAG";
    public const string GasTypeTag = "GAS_TYPE";

    /// <summary>One cylinder holds 10 kg at most; a full pair is refused, not overflowed.</summary>
    public const int MaxGramsPerItem = 10000;

    public static void Init(GameItem item, string gasType)
    {
        // The 16x48 artwork fills one column, so the stack reserves exactly 1x3.
        item.SetShape(new GridShapeBuilder(1, 3).SetData(new byte[] { 1, 1, 1 }, 1));
        item.EnableTag(GasContainerTag, skipValidate: true);
        // Reads use modifiedState in the game; publish both tags before using them.
        item.ModifyTag(GasTypeTag, state => state.Enable().SetString(gasType ?? ""));
        // Shop/loot cylinders start with 100 g; machine outputs set their own mass.
        item.SetUnitCount(100);
        item.onChangeAmountItemFunc += ProductQuality.RefreshValue;
        ProductQuality.RefreshValue(item);
        MakeSplittable(item);
    }

    /// <summary>
    /// Equal gases stack into one slot only while the combined mass stays within one item;
    /// the pair is refused rather than partly merged, matching the feed piece rule.
    /// </summary>
    public static void MakeStackable(GameItem item)
    {
        item.mayStackItemFunc = (self, other) =>
            other != null &&
            IsGasContainer(other) &&
            string.Equals(GetGasType(self), GetGasType(other), StringComparison.OrdinalIgnoreCase) &&
            (long)self.unitCount + other.unitCount <= MaxGramsPerItem;
        item.maxSlotAmountItemFunc = (_, _, _) => MaxGramsPerItem;
    }

    /// <summary>Double-click halves a stack, the gesture the feed pieces use to split.</summary>
    public static void MakeSplittable(GameItem item)
    {
        item.mayActivateSlotItemFunc = (self, _, _) => GeneralHelper.IsItemOwned(self) && CanSplit(self);
        item.canActivateSlotItemFunc = (self, _, _) => GeneralHelper.IsItemOwned(self) && CanSplit(self);
        item.onActivateSlotItemFunc = (self, _, _) => Split(self);
    }

    public static bool CanSplit(GameItem? item)
    {
        return item != null && item.unitCount >= 2;
    }

    /// <summary>
    /// Moves half the mass into a fresh piece. The native split deducts the source first,
    /// so when no slot accepts the piece the grams go back instead of vanishing.
    /// </summary>
    private static void Split(GameItem item)
    {
        if (!CanSplit(item))
        {
            return;
        }
        int half = item.unitCount / 2;
        GameItem? piece = item.GetSplitItem(half);
        if (piece == null)
        {
            item.SetUnitCount(item.unitCount + half);
            ProductQuality.RefreshValue(item);
            return;
        }
        // The inventory search pours a loose item into a matching pile before it looks for
        // free space, which would put the piece straight back into the source stack.
        Func<GameItem, GameItem, bool>? stack = piece.mayStackItemFunc;
        piece.mayStackItemFunc = null!;
        GraphUtils.EmporiumTryAdd(item.parentInventory, piece);
        piece.mayStackItemFunc = stack!;
        if (piece.parentInventory == null)
        {
            item.SetUnitCount(item.unitCount + half);
            ProductQuality.RefreshValue(item);
            return;
        }
        AudioManager.Instance.Play3("click_vintage");
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
