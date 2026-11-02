using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace ElementalIndustry;

/// <summary>Extend the original furnace only while its chemistry module is installed.</summary>
internal static class FurnaceChemistry
{
    internal const string ModuleId = "elem_furnace_chem_module";
    /// <summary>Raises the furnace's 8 base power to 12; furnace recipes pay the current draw.</summary>
    internal const int ModuleEfficiency = -50;

    private static bool HasModule(GameInventory modules) =>
        GraphUtils.FindAllChildrenType<GameItem>(modules).Any(item => item.identifier == ModuleId);

    private static readonly MachineDefinition FurnaceDefinition = new()
    {
        id = "furnace", power = 8, moduleBay = true
    };

    [HarmonyPatch(typeof(MachineFurnace), nameof(MachineFurnace.Furnace))]
    internal static class FurnaceFactoryPatch
    {
        private static void Postfix(GameItem __result)
        {
            if (MachineHelper.GetBodyStorage(__result, 0) is not GameSlotInventory battery ||
                MachineHelper.GetBodyStorage(__result, 1) is not GameGridInventory modules ||
                MachineHelper.GetBodyStorage(__result, 2) is not GameGridInventory input ||
                MachineHelper.GetBodyStorage(__result, 3) is not GameGridInventory output) return;
            var unusedWaterSlot = new GameSlotInventory();
            var originalFilter = input.mayInventoryAddItemFunc;
            input.mayInventoryAddItemFunc = (item, inv) =>
                (originalFilter?.Invoke(item, inv) ?? false) ||
                (item != null && GeneralHelper.IsItemOwned(item) && HasModule(modules) &&
                 (item.identifier == "elem_iron_oxide" || item.identifier == "elem_coke" ||
                  item.identifier == "elem_silica" || item.identifier == "elem_silicon" ||
                  item.identifier == "junk"));
            var originalCycle = __result.onCycleEndSlotItemFunc;
            __result.onCycleEndSlotItemFunc = (item, inventory, slot) =>
            {
                if (!HasModule(modules))
                {
                    originalCycle?.Invoke(item, inventory, slot);
                    return;
                }
                var outputFilter = output.mayInventoryAddItemFunc;
                try
                {
                    output.mayInventoryAddItemFunc = (candidate, _) => candidate != null &&
                        GeneralHelper.IsItemOwned(candidate) && Processor.CanAcceptOutput(FurnaceDefinition, candidate);
                    if (Processor.Run(item, FurnaceDefinition, battery, unusedWaterSlot, input, output, modules)) return;
                }
                finally
                {
                    output.mayInventoryAddItemFunc = outputFilter;
                }
                // Iron oxide smelts 1:1 through the mod recipes above; the native cycle
                // only sees vanilla ore and scrap, with coke as an optional purity boost.
                GameItem? coke = GraphUtils.FindAllChildrenType<GameItem>(input).FirstOrDefault(x =>
                    x.identifier == Coke.ItemId && DurabilityHelper.GetRealDurability(x) >= Coke.FurnaceBoostWear);
                var beforeOutput = new HashSet<GameItem>(GraphUtils.FindAllChildrenType<GameItem>(output));
                originalCycle?.Invoke(item, inventory, slot);
                if (coke != null && BoostProducedIngots(output, beforeOutput) > 0) Processor.Wear(coke, Coke.FurnaceBoostWear);
            };
        }
    }

    /// <summary>
    /// Coke raises the ingot one purity grade above what the native furnace computed, and the
    /// number of boosted ingots decides whether the coke is worn.
    /// <para>
    /// This deliberately does not patch <see cref="IngotPurityHelper"/>: that type builds its
    /// static <c>ingotPurities</c> table (including the localized <c>ItemCondition.display</c>
    /// strings) the first time it is touched. A Harmony patch targets the type at load time,
    /// long before the language tables are ready, so every ingot of the session — including the
    /// vanilla metal ingot — would bake the English strings and serialize them into the save.
    /// The native cycle always calls <c>SetPurityFromInput</c> before the ingot reaches the
    /// output grid, so adding one grade afterwards is equivalent to the old flux-level bump.
    /// </para>
    /// </summary>
    private static int BoostProducedIngots(GameGridInventory output, HashSet<GameItem> before)
    {
        int produced = 0;
        foreach (GameItem ingot in GraphUtils.FindAllChildrenType<GameItem>(output))
        {
            if (before.Contains(ingot) || !ingot.IsTag("INGOT_PURITY_TAG")) continue;
            produced++;
            IngotPurityHelper.SetPurity(ingot,
                IngotPurityHelper.GetPurityConst(IngotPurityHelper.GetPurity(ingot), 1));
        }
        return produced;
    }
}
