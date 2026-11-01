using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace ElementalIndustry;

/// <summary>Extend the original furnace only while its chemistry module is installed.</summary>
internal static class FurnaceChemistry
{
    internal const string ModuleId = "elem_furnace_chem_module";

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
                var contents = GraphUtils.FindAllChildrenType<GameItem>(input);
                var oxide = contents.Where(x => x.identifier == "elem_iron_oxide").ToList();
                GameItem? coke = contents.FirstOrDefault(x => x.identifier == "elem_coke");
                var beforeOutput = new HashSet<GameItem>(GraphUtils.FindAllChildrenType<GameItem>(output));
                try
                {
                    // The native furnace selects ore by ID. Temporarily present oxide as
                    // common ore so the native input, power, module and output rules apply.
                    foreach (GameItem ore in oxide) ore.identifier = "common_ore";
                    originalCycle?.Invoke(item, inventory, slot);
                    if (coke != null && BoostProducedIngots(output, beforeOutput) > 0) GasContainer.Consume(coke);
                }
                finally
                {
                    foreach (GameItem ore in oxide)
                        if (ore is not GameItemElement element || !element.IsDestroyed()) ore.identifier = "elem_iron_oxide";
                }
            };
        }
    }

    /// <summary>
    /// Coke raises the ingot one purity grade above what the native furnace computed, and the
    /// number of boosted ingots decides whether a coke is consumed.
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

    /// <summary>
    /// 可乐作为有限耐久的水过滤材料。
    /// <para>
    /// 这里刻意<strong>不</strong>加 <c>[HarmonyPatch]</c>：加载器在 <c>ModLoader.Awake</c> 阶段
    /// 会对整个模组程序集 <c>PatchAll</c>，而 JIT <see cref="WaterHelper.HandleFilter"/> 会提前执行
    /// <c>Liquid</c> 的静态构造——那时 locale 还是默认 en，液体显示名会被固化成英文并随存档保存。
    /// 因此改由 <see cref="ModEntry"/> 在语言就绪后手动装配。
    /// </para>
    /// </summary>
    internal static class CokeFilterPatch
    {
        internal static void Apply(Harmony harmony)
        {
            harmony.Patch(
                AccessTools.Method(typeof(WaterHelper), nameof(WaterHelper.HandleFilter)),
                postfix: new HarmonyMethod(typeof(CokeFilterPatch), nameof(Postfix)));
        }

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
