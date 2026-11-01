using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace ElementalIndustry;

/// <summary>Native module quality affects the value of ungraded products; graded ingots use native purity.</summary>
internal static class ProductQuality
{
    private const string QualityTag = "ELEM_PRODUCT_QUALITY_INT";

    internal static int Get(GameItem item) => item.GetTagReadonly(QualityTag).GetInt();

    internal static void Apply(GameItem item, int quality)
    {
        if (quality == 0) return;
        Set(item, quality);
    }

    internal static void Set(GameItem item, int quality)
    {
        item.ModifyTag(QualityTag, state => state.Enable().SetInt(Math.Clamp(quality, -100, 300)));
        RefreshValue(item);
    }

    internal static int Weighted(int oldQuality, int oldMass, int addedQuality, int addedMass)
    {
        long total = (long)oldMass + addedMass;
        if (total <= 0) return 0;
        return (int)(((long)oldQuality * oldMass + (long)addedQuality * addedMass) / total);
    }

    internal static void RefreshValue(GameItem item)
    {
        bool gas = GasContainer.IsGasContainer(item);
        if ((!gas && !item.IsTag(QualityTag)) || !ContentDatabase.Items.TryGetValue(item.identifier, out ItemDefinition? definition)) return;
        long amount = gas ? Math.Max(0, item.unitCount) : 1;
        long divisor = gas ? 10000 : 100;
        long factor = Math.Clamp(100 + Get(item), 0, 400);
        // Gas definition prices are per 100 g. The game's store price is per item,
        // so each merged stack must be priced by its total mass.
        item.unitValue = definition.value * amount * factor / divisor;
    }

    [HarmonyPatch(typeof(GameItem), nameof(GameItem.GetCurrentValue))]
    private static class CurrentValuePatch { private static void Prefix(GameItem __instance) => RefreshValue(__instance); }

    [HarmonyPatch(typeof(GameItem), nameof(GameItem.GetNegociatedValue))]
    private static class NegotiatedValuePatch { private static void Prefix(GameItem __instance) => RefreshValue(__instance); }

    [HarmonyPatch(typeof(GameItem), nameof(GameItem.GetBaseValue))]
    private static class BaseValuePatch { private static void Prefix(GameItem __instance) => RefreshValue(__instance); }

    [HarmonyPatch(typeof(GameItem), nameof(GameItem.GetRefreshedValue))]
    private static class RefreshedValuePatch { private static void Prefix(GameItem __instance) => RefreshValue(__instance); }

    [HarmonyPatch(typeof(GameItem), nameof(GameItem.GetValue))]
    private static class LegacyValuePatch
    {
        private static readonly MethodInfo ValueModifier = typeof(GameItem).GetMethod("ValueModifier", BindingFlags.NonPublic | BindingFlags.Static)!;
        private static bool Prefix(GameItem __instance, List<GameCharacterItem> party, GameCharacterItem vendor, bool singleValue, ref long __result)
        {
            if (!GasContainer.IsGasContainer(__instance)) return true;
            RefreshValue(__instance);
            __result = (long)ValueModifier.Invoke(null, new object?[] { __instance, party, vendor })!;
            if (singleValue) __result /= Math.Max(1, __instance.unitCount);
            return false;
        }
    }

    private readonly struct GasMerge
    {
        internal readonly bool applicable;
        internal readonly int oldQuality, oldMass, addedQuality, addedMass;
        internal GasMerge(int oldQuality, int oldMass, int addedQuality, int addedMass)
        { applicable = true; this.oldQuality = oldQuality; this.oldMass = oldMass; this.addedQuality = addedQuality; this.addedMass = addedMass; }
    }

    [HarmonyPatch(typeof(SlotMarker), nameof(SlotMarker.StackItemUnchecked))]
    private static class GasMergePatch
    {
        private static void Prefix(GameItem item, GameItem targetItem, int amount, out GasMerge __state)
        {
            __state = default;
            if (!GasContainer.IsGasContainer(item) || !GasContainer.IsGasContainer(targetItem) ||
                !string.Equals(GasContainer.GetGasType(item), GasContainer.GetGasType(targetItem), StringComparison.OrdinalIgnoreCase)) return;
            int transferred = amount < 0 ? item.unitCount : Math.Min(amount, item.unitCount);
            __state = new GasMerge(Get(targetItem), targetItem.unitCount, Get(item), transferred);
        }

        private static void Postfix(GameItem targetItem, GasMerge __state)
        {
            if (__state.applicable)
                Set(targetItem, Weighted(__state.oldQuality, __state.oldMass, __state.addedQuality, __state.addedMass));
        }
    }

    [HarmonyPatch(typeof(GameItem), nameof(GameItem.GetSplitItem))]
    private static class GasSplitPatch
    {
        private static void Postfix(GameItem __instance, GameItem __result)
        {
            if (GasContainer.IsGasContainer(__instance) && __result != null)
                Set(__result, Get(__instance));
        }
    }
}
