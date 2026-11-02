using System.Linq;

namespace ElementalIndustry;

/// <summary>
/// Coke is a durable tool rather than a filter cartridge: furnace boosts and direct water
/// treatment both wear the same durability, and the item is used up at zero.
/// </summary>
internal static class Coke
{
    internal const string ItemId = "elem_coke";
    internal const int MaxDurability = 100;
    internal const int FurnaceBoostWear = 10;
    /// <summary>Contaminant volume (native µL parts) adsorbed per durability point: 2 mL.</summary>
    internal const int AdsorbedPerPoint = 2000;
    internal static readonly string[] Adsorbed = { "chemical_contaminant", "heavy_metal" };

    internal static void Configure(GameItem item)
    {
        item.SetGameItemType("WATER_PURIFICATION_SUPPLY");
        DurabilityHelper.InitItemWithDurability(item, MaxDurability);
        item.maySelectSlotItemFunc = (self, _, _) => GeneralHelper.IsItemOwned(self);
        item.canSelectSlotItemFunc = (self, _, _) => GeneralHelper.IsItemOwned(self);
        item.mayThisTargetItemFunc = CanAdsorb;
        item.canThisTargetItemFunc = CanAdsorb;
        item.onThisTargetItemFunc = Adsorb;
    }

    internal static bool CanAdsorb(GameItem coke, GameItem target)
    {
        if (coke == null || target == null || !GeneralHelper.IsItemOwned(coke) || !GeneralHelper.IsItemOwned(target) ||
            !target.IsTag("LIQUID_CONTAINER_TAG") || target.unitCount != 1 || DurabilityHelper.GetRealDurability(coke) <= 0)
        {
            return false;
        }
        return target.GetTagReadonly(Liquid.GetCurrentPartFromId("water")).GetInt() > 0 &&
               Adsorbed.Any(id => WaterHelper.GetContaminantAmount(target, id) > 0);
    }

    /// <summary>Adsorbs chemical contamination and heavy metals; it does not kill microbes.</summary>
    internal static void Adsorb(GameItem coke, GameItem target)
    {
        if (!CanAdsorb(coke, target)) return;
        int capacity = DurabilityHelper.GetRealDurability(coke) * AdsorbedPerPoint;
        int total = 0;
        foreach (string id in Adsorbed)
        {
            if (total >= capacity) break;
            total += WaterHelper.RemoveContaminantFromContainer(target, id, capacity - total, 0);
        }
        if (total <= 0) return;
        // Like the native purifier, the removed volume is made up with water.
        WaterHelper.AddLiquid(target, "water", total);
        WaterFeatureHelper.UpdateWaterFeature(target, isExpose: false);
        SpriteHelper.UpdateWaterContainerSprite(target);
        Processor.Wear(coke, (total + AdsorbedPerPoint - 1) / AdsorbedPerPoint);
    }
}
