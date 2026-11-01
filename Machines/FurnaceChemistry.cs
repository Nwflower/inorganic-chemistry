using System;
using System.Linq;
using HarmonyLib;

namespace ElementalIndustry;

/// <summary>Extend the original furnace only while its chemistry module is installed.</summary>
internal static class FurnaceChemistry
{
    internal const string ModuleId = "elem_furnace_chem_module";
    [ThreadStatic] private static bool _boostPurity;
    [ThreadStatic] private static int _ingotsProduced;

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
                  item.identifier == "elem_silica" || item.identifier == "junk"));
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
                var contents = GraphUtils.FindAllChildrenType<GameItem>(input);
                var oxide = contents.Where(x => x.identifier == "elem_iron_oxide").ToList();
                GameItem? coke = contents.FirstOrDefault(x => x.identifier == "elem_coke");
                bool priorBoost = _boostPurity;
                int priorProduced = _ingotsProduced;
                try
                {
                    // The native furnace selects ore by ID. Temporarily present oxide as
                    // common ore so the native input, power, module and output rules apply.
                    foreach (GameItem ore in oxide) ore.identifier = "common_ore";
                    _boostPurity = coke != null;
                    _ingotsProduced = 0;
                    originalCycle?.Invoke(item, inventory, slot);
                    if (coke != null && _ingotsProduced > 0) GasContainer.Consume(coke);
                }
                finally
                {
                    foreach (GameItem ore in oxide)
                        if (ore is not GameItemElement element || !element.IsDestroyed()) ore.identifier = "elem_iron_oxide";
                    _boostPurity = priorBoost;
                    _ingotsProduced = priorProduced;
                }
            };
        }
    }

    [HarmonyPatch(typeof(IngotPurityHelper), nameof(IngotPurityHelper.SetPurityFromInput))]
    internal static class PurityPatch
    {
        private static void Prefix(ref int fluxLevel)
        {
            if (!_boostPurity) return;
            fluxLevel++;
            _ingotsProduced++;
        }
    }

    [HarmonyPatch(typeof(WaterHelper), nameof(WaterHelper.HandleFilter))]
    internal static class CokeFilterPatch
    {
        private static void Postfix(string identifier, GameItem filter, ref int __result)
        {
            if (filter == null || filter.identifier != "elem_coke" ||
                identifier != "chemical_contaminant" || __result <= 0) return;
            int durability = Math.Max(0, DurabilityHelper.GetRealDurability(filter));
            int removed = (int)Math.Min((long)__result, (long)durability * 1000);
            if (removed == 0) return;
            DurabilityHelper.ChangeDurability(filter, -(int)(((long)removed + 999) / 1000));
            __result -= removed;
        }
    }
}
