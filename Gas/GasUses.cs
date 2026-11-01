using System;

namespace ElementalIndustry;

/// <summary>
/// Extra uses for gas cylinders beyond machines: oxygen can aerate water and screen
/// seed traits. These are deliberately direct item interactions, not a new liquid.
/// </summary>
internal static class GasUses
{
    public static void ConfigureOxygenCylinder(GameItem item)
    {
        item.maySelectSlotItemFunc = (source, _, _) => GeneralHelper.IsItemOwned(source);
        item.canSelectSlotItemFunc = (source, _, _) => GeneralHelper.IsItemOwned(source);

        item.mayThisTargetItemFunc = (source, target) => IsWater(target) || IsSeed(target);
        item.canThisTargetItemFunc = (source, target) => IsWater(target) || IsSeed(target);
        item.onThisTargetItemFunc = (source, target) =>
        {
            if (IsWater(target))
            {
                AerateWater(source, target);
            }
            else if (IsSeed(target))
            {
                ScreenSeed(source, target);
            }
        };
    }

    private static bool IsWater(GameItem target)
    {
        return target != null
            && target.IsTag("LIQUID_CONTAINER_TAG")
            && !WaterHelper.IsEmpty(target);
    }

    private static bool IsSeed(GameItem target)
    {
        return target != null
            && target.IsTag("SEED_ITEM_TAG")
            && HydroponicHelper.GetPlant(target) != null
            && !target.IsTag(Ids.ScreenedSeedTag);
    }

    private static void AerateWater(GameItem oxygen, GameItem water)
    {
        if (!GasContainer.Is(oxygen, "O2"))
        {
            return;
        }
        int total = WaterHelper.GetTotalVolume(water);
        int dose = Math.Max(1000, total / 10);
        int removed = 0;
        removed += WaterHelper.RemoveContaminantFromContainer(water, "organic_waste", dose, 0);
        removed += WaterHelper.RemoveContaminantFromContainer(water, "microbe", dose, 0);
        if (removed <= 0)
        {
            return;
        }

        WaterHelper.AddLiquid(water, "water", removed);
        WaterFeatureHelper.UpdateWaterFeature(water, isExpose: false);
        SpriteHelper.UpdateWaterContainerSprite(water);
        AudioHelper.PourWater();
        GasContainer.Consume(oxygen);
    }

    private static void ScreenSeed(GameItem oxygen, GameItem seed)
    {
        if (!GasContainer.Is(oxygen, "O2"))
        {
            return;
        }
        seed.EnableTag(Ids.ScreenedSeedTag);
        AudioManager.Instance.Play3("scanner_activate");
        GasContainer.Consume(oxygen);
    }
}
