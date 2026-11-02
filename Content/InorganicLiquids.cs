using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace ElementalIndustry;

/// <summary>Register with the local game's public liquid tables after locale initialization.</summary>
internal static class InorganicLiquids
{
    private static readonly List<Liquid> Owned = new();
    private static bool _patched;

    internal static void Register(Harmony harmony)
    {
        // No official AddLiquid API exists in this target build. Both collections
        // must contain the same instance for pouring, scanner and purification.
        Add(InorganicReactions.Hcl, "ad8d78", .1);
        Add(InorganicReactions.Nitric, "b5a16c", .1);
        Add(InorganicReactions.Caustic, "829baa", .1);
        Add(InorganicReactions.Ammonia, "7f9c91", .08);
        Add(InorganicReactions.Nitrate, "a6ac87", .02);
        Add(InorganicReactions.Peroxide, "96aeb4", .04);
        Add(InorganicReactions.Hypochlorite, "ada875", .1);
        if (_patched) return;
        harmony.Patch(AccessTools.Method(typeof(ChemicalHelper), nameof(ChemicalHelper.HandleTemperatureReaction)),
            prefix: new HarmonyMethod(typeof(InorganicLiquids), nameof(ReactPrefix)),
            postfix: new HarmonyMethod(typeof(InorganicLiquids), nameof(ReactPostfix)));
        harmony.Patch(AccessTools.Method(typeof(HydroponicHelper), nameof(HydroponicHelper.HandleNutriment)),
            prefix: new HarmonyMethod(typeof(InorganicLiquids), nameof(NutrimentPrefix)),
            finalizer: new HarmonyMethod(typeof(InorganicLiquids), nameof(NutrimentFinalizer)));
        harmony.Patch(AccessTools.Method(typeof(HydroponicHelper), nameof(HydroponicHelper.HandleContaminant)),
            prefix: new HarmonyMethod(typeof(InorganicLiquids), nameof(ContaminantPrefix)),
            finalizer: new HarmonyMethod(typeof(InorganicLiquids), nameof(ContaminantFinalizer)));
        harmony.Patch(AccessTools.Method(typeof(ChemicalHelper), nameof(ChemicalHelper.DissolveInto)),
            prefix: new HarmonyMethod(typeof(InorganicLiquids), nameof(DissolvePrefix)));
        harmony.Patch(AccessTools.Method(typeof(GameItem), nameof(GameItem.GetSplitItem)),
            postfix: new HarmonyMethod(typeof(InorganicLiquids), nameof(SplitPostfix)));
        _patched = true;
    }

    private static void Add(string id, string color, double lethality)
    {
        if (Liquid.LiquidDict.ContainsKey(id)) return;
        var liquid = new Liquid
        {
            identifier = id, part = "PART_" + id.ToUpperInvariant(),
            currentPart = "CURRENT_PART_" + id.ToUpperInvariant(),
            displayName = Loc.Get("liquid." + id), color = color, lethality = lethality,
            particleSize = 5, baseMaxPurityFromMachine = 12, basePurifierRemoveAmount = 4000,
            // Solute-equivalent components stay behind when the native evaporator
            // removes solvent. This is an explicit gameplay simplification.
            isEvaporative = false, canBeRemovedByPurifier = true,
        };
        Liquid.Liquids.Add(liquid);
        Liquid.LiquidDict.Add(id, liquid);
        Owned.Add(liquid);
    }

    internal static void Unregister()
    {
        foreach (Liquid liquid in Owned)
        {
            Liquid.Liquids.Remove(liquid);
            Liquid.IngestableLiquids.Remove(liquid);
            if (Liquid.LiquidDict.TryGetValue(liquid.identifier, out var current) && ReferenceEquals(current, liquid))
                Liquid.LiquidDict.Remove(liquid.identifier);
        }
        Owned.Clear();
        _patched = false;
    }

    private static int Get(GameItem item, string id)
        => Math.Max(0, item.GetTagReadonly(Liquid.GetCurrentPartFromId(id)).GetInt());
    private static void Set(GameItem item, string id, int amount)
        => item.ModifyTag(Liquid.GetCurrentPartFromId(id), state => state.Enable().SetInt(amount), skipValidate: true);

    private static void ReactPrefix(GameItem? container, out bool __state)
    {
        __state = container != null && ContentDatabase.IsModuleEnabled("ChemicalProcessing") && HasReagents(container);
        if (!__state || container == null) return;
        var parts = Liquid.Liquids.ToDictionary(liquid => liquid.identifier, liquid => Get(container, liquid.identifier));
        InorganicReactions.Tick(parts);
        foreach (var part in parts)
            if (part.Value != Get(container, part.Key)) Set(container, part.Key, part.Value);
    }

    private static void ReactPostfix(GameItem container, bool __state)
    {
        if (!__state) return;
        // Lend only the new reagents to the native acid/base effects, then restore
        // their identities and retain exactly the amounts consumed by the game.
        var acids = new ReagentLease(container, "acid", InorganicReactions.Acids, includeNative: false);
        var bases = new ReagentLease(container, "base", InorganicReactions.Bases, includeNative: false);
        try
        {
            if (acids.Amount + bases.Amount > 0)
            {
                ChemicalHelper.HandleOrganicWasteMineralization(container);
                ChemicalHelper.HandleAcidBaseMicrobeKill(container);
                ChemicalHelper.HandleStabilizingAgentSedimentation(container);
                ChemicalHelper.HandleProteinPrecipitation(container);
                ChemicalHelper.HandleLipobondAgentSedimentation(container);
            }
        }
        finally { bases.Restore(); acids.Restore(); }
        WaterFeatureHelper.UpdateWaterFeature(container, isExpose: false);
        SpriteHelper.UpdateWaterContainerSprite(container);
    }

    // Native hydroponics reads and consumes "mineral". Pool nitrogen nutrients
    // only for this call; pouring, scanning and finisher recipes retain nitrate.
    private static void NutrimentPrefix(GameItem growingSeed, out ReagentLease? __state)
    {
        __state = null;
        if (!ContentDatabase.IsModuleEnabled("ChemicalProcessing")) return;
        GameItem machine = HydroponicHelper.GetAssociatedHydroponic(growingSeed);
        GameItem? water = machine == null ? null : MachineHydroponic.GetWaterSlotItem(machine);
        if (water != null) __state = new ReagentLease(water, "mineral", new[] { InorganicReactions.Nitrate }, true);
    }
    private static void NutrimentFinalizer(ReagentLease? __state) => __state?.Restore();

    private static void ContaminantPrefix(GameItem growingSeed, out ReagentLease[]? __state)
    {
        __state = null;
        if (!ContentDatabase.IsModuleEnabled("ChemicalProcessing")) return;
        GameItem machine = HydroponicHelper.GetAssociatedHydroponic(growingSeed);
        GameItem? water = machine == null ? null : MachineHydroponic.GetWaterSlotItem(machine);
        if (water != null) __state = new[]
        {
            new ReagentLease(water, "acid", InorganicReactions.Acids, true),
            new ReagentLease(water, "base", InorganicReactions.Bases, true),
        };
    }
    private static void ContaminantFinalizer(ReagentLease[]? __state)
    {
        if (__state != null) foreach (var lease in __state) lease.Restore();
    }

    private static bool DissolvePrefix(ref GameItem chemicalItem, GameItem liquidContainer)
    {
        if (!ContentDatabase.Items.ContainsKey(chemicalItem.identifier)) return true;
        if (ChemicalHelper.GetTotalVolume(chemicalItem) > WaterHelper.GetFreeCapacity(liquidContainer)) return false;
        // The native helper destroys its input. Split exactly one dose first so
        // the simplified laboratory stacks keep their remaining doses.
        if (chemicalItem.unitCount > 1) chemicalItem = chemicalItem.GetSplitItem(1);
        return true;
    }

    private static void SplitPostfix(GameItem __instance, GameItem __result)
    {
        if (__result == null || !__instance.IsTag("CHEM_ITEM_TAG") ||
            !ContentDatabase.Items.ContainsKey(__instance.identifier)) return;
        // Native splitting constructs a fresh directory item; it does not copy composition.
        foreach (Liquid liquid in Liquid.Liquids)
            Set(__result, liquid.identifier, Get(__instance, liquid.identifier));
        ChemicalProductHelper.GradeProduct(__result);
    }

    private sealed class ReagentLease
    {
        private readonly GameItem _item;
        private readonly string _native;
        private readonly string[] _ids;
        private readonly int[] _before;
        private readonly int _savedNative;
        private readonly bool _includeNative;
        internal int Amount { get; }
        internal ReagentLease(GameItem item, string native, string[] ids, bool includeNative)
        {
            _item = item; _native = native; _ids = ids; _includeNative = includeNative;
            _savedNative = Get(item, native);
            _before = ids.Select(id => Get(item, id)).ToArray();
            Amount = _before.Sum();
            Set(item, native, checked(Amount + (includeNative ? _savedNative : 0)));
            foreach (string id in ids) Set(item, id, 0);
        }
        internal void Restore()
        {
            long total = (long)Amount + (_includeNative ? _savedNative : 0);
            int remaining = (int)Math.Min(total, Get(_item, _native));
            int restored = 0;
            for (int i = 0; i < _ids.Length; i++)
            {
                int share = total == 0 ? 0 : (int)((long)remaining * _before[i] / total);
                Set(_item, _ids[i], share);
                restored += share;
            }
            // Integer rounding remainder stays in the last custom component when
            // native reagent was excluded, rather than creating generic acid/base.
            if (!_includeNative && _ids.Length > 0)
            {
                Set(_item, _ids[_ids.Length - 1], Get(_item, _ids[_ids.Length - 1]) + remaining - restored);
                Set(_item, _native, _savedNative);
            }
            else Set(_item, _native, remaining - restored);
        }
    }

    private static bool HasReagents(GameItem item) => Owned.Any(liquid => Get(item, liquid.identifier) > 0);
}
