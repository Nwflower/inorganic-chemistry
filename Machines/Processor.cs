using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ElementalIndustry;

internal enum ProcessFailure
{
    None,
    NoRecipesEnabled,
    NoCapacity,
    MissingInputs,
    MissingWater,
    MissingPower,
    OutputSpace,
    OutputUnavailable,
}

internal static class Processor
{
    private const string LastBatchesTag = "ELEM_PROCESS_LAST_BATCHES_INT";
    private const string LastFailureTag = "ELEM_PROCESS_LAST_FAILURE_INT";

    internal static int LastBatches(GameItem machine)
        => Math.Max(0, machine.GetTagReadonly(LastBatchesTag).GetInt());

    internal static ProcessFailure LastFailure(GameItem machine)
    {
        int value = machine.GetTagReadonly(LastFailureTag).GetInt();
        return Enum.IsDefined(typeof(ProcessFailure), value) ? (ProcessFailure)value : ProcessFailure.None;
    }

    internal static void NoteSelection(GameItem machine, IEnumerable<RecipeDefinition> recipes)
    {
        bool anyEnabled = recipes.Any(recipe => RecipeSelection.IsEnabled(machine, recipe));
        WriteStatus(machine, 0, anyEnabled ? ProcessFailure.None : ProcessFailure.NoRecipesEnabled);
    }

    private static void WriteStatus(GameItem machine, int batches, ProcessFailure failure)
    {
        machine.ModifyTag(LastBatchesTag, state => state.Enable().SetInt(batches));
        machine.ModifyTag(LastFailureTag, state => state.Enable().SetInt((int)failure));
    }

    public static bool CanAcceptInput(MachineDefinition machine, GameItem item)
    {
        foreach (RecipeDefinition recipe in ContentDatabase.RecipesFor(machine.id))
        {
            if (recipe.valueRoll != null && ModuleAssembly.IsMaterial(recipe.valueRoll, item))
            {
                return true;
            }
            foreach (ItemAmountDefinition input in recipe.inputs)
            {
                if (Matches(input, item))
                {
                    return true;
                }
            }
        }
        return false;
    }

    public static bool CanAcceptOutput(MachineDefinition machine, GameItem item)
    {
        foreach (RecipeDefinition recipe in ContentDatabase.RecipesFor(machine.id))
        {
            if (recipe.outputs.Any(output => string.Equals(output.id, item.identifier, StringComparison.Ordinal) ||
                    output.roll.Any(entry => string.Equals(entry.id, item.identifier, StringComparison.Ordinal))) ||
                (recipe.valueRoll != null && ModuleAssembly.IsPenaltyOutput(item)))
            {
                return true;
            }
        }
        return false;
    }

    public static bool Run(
        GameItem machine,
        MachineDefinition definition,
        GameSlotInventory batterySlot,
        GameSlotInventory waterSlot,
        GameGridInventory inputGrid,
        GameGridInventory outputGrid,
        GameGridInventory moduleGrid)
    {
        if (definition.id == Ids.RustExtractor)
            return RunRustExtractor(machine, definition, batterySlot, waterSlot, outputGrid, moduleGrid);

        RecipeDefinition[] recipes = ContentDatabase.RecipesFor(definition.id)
            .Where(recipe => definition.id == Ids.ElectrolysisCell ||
                (definition.id == Ids.ChemicalReactor
                    ? recipe.id == RecipeSelection.Selected(machine, ContentDatabase.RecipesFor(definition.id))
                    : RecipeSelection.IsEnabled(machine, recipe)))
            .ToArray();
        if (recipes.Length == 0)
        {
            WriteStatus(machine, 0, ProcessFailure.NoRecipesEnabled);
            return false;
        }

        int batches = 0;
        ProcessFailure failure = ProcessFailure.None;
        // Electrolysis handles a continuously scaled volume in one nightly run.
        // The module assembler spends every accepted material on one module per click.
        int cycles = definition.id == Ids.ElectrolysisCell || definition.id == Ids.ChipFab
            ? 1 : MachineFactory.WorkCycles(machine);
        if (cycles <= 0)
        {
            WriteStatus(machine, 0, ProcessFailure.NoCapacity);
            return false;
        }

        for (int cycle = 0; cycle < cycles; cycle++)
        {
            bool ran = false;
            ProcessFailure cycleFailure = ProcessFailure.MissingInputs;
            foreach (RecipeDefinition recipe in recipes)
            {
                if (definition.id == Ids.ElectrolysisCell && recipe.id == "chlor_alkali" &&
                    (waterSlot.childItem == null || WaterHelper.GetContaminantAmount(waterSlot.childItem, "mineral") < recipe.water))
                    continue;
                ProcessFailure result = TryRunRecipe(machine, definition, recipe, batterySlot, waterSlot, inputGrid, outputGrid, moduleGrid);
                if (result == ProcessFailure.None)
                {
                    ran = true;
                    batches++;
                    break;
                }
                cycleFailure = MoreActionable(cycleFailure, result);
            }
            if (!ran)
            {
                failure = cycleFailure;
                break;
            }
        }
        WriteStatus(machine, batches, failure);
        return batches > 0;
    }

    private static bool RunRustExtractor(
        GameItem machine,
        MachineDefinition definition,
        GameSlotInventory batterySlot,
        GameSlotInventory waterSlot,
        GameGridInventory outputGrid,
        GameGridInventory moduleGrid)
    {
        RecipeDefinition[] recipes = ContentDatabase.RecipesFor(definition.id)
            .Where(recipe => !string.IsNullOrEmpty(recipe.waterContaminant))
            .ToArray();
        int cycles = MachineFactory.WorkCycles(machine);
        int batches = 0;
        ProcessFailure failure = ProcessFailure.None;
        for (int cycle = 0; cycle < cycles; cycle++)
        {
            GameItem? water = waterSlot.childItem;
            if (water == null || water.unitCount != 1)
            {
                failure = ProcessFailure.MissingWater;
                break;
            }

            // Each contaminant has its own recipe and threshold. The extractor runs
            // every pathway it can satisfy, so the player never chooses a filter.
            var runs = new List<(RecipeDefinition recipe, string contaminant, int batches, int amount)>();
            foreach (RecipeDefinition recipe in recipes)
            {
                int available = WaterHelper.GetContaminantAmount(water, recipe.waterContaminant);
                int count = recipe.water <= 0 ? 0 : available / recipe.water;
                if (count > 0) runs.Add((recipe, recipe.waterContaminant, count, count * recipe.water));
            }
            if (runs.Count == 0)
            {
                failure = ProcessFailure.MissingWater;
                break;
            }

            int power = runs.Sum(run => RecipePower(machine, definition, run.recipe) * run.batches);
            GameItem? battery = batterySlot.childItem;
            if (!CanDraw(battery, power))
            {
                failure = ProcessFailure.MissingPower;
                break;
            }

            var outputs = new List<GameItem>();
            foreach (var run in runs)
            {
                for (int i = 0; i < run.batches; i++)
                {
                    GameItem? produced = TryCreateOutput(run.recipe.outputs[0], machine);
                    if (produced != null) outputs.Add(produced);
                }
            }
            if (outputs.Count != runs.Sum(run => run.batches))
            {
                MachineFactory.DestroyAll(outputs);
                failure = ProcessFailure.OutputUnavailable;
                break;
            }
            if (!MachineFactory.TryPlaceOutputs(outputGrid, outputs, out Action rollbackOutputs))
            {
                failure = ProcessFailure.OutputSpace;
                break;
            }
            if (battery == null || battery.unitCount != 1 || !PowerHelper.DrawPowerSource(battery, power))
            {
                rollbackOutputs();
                failure = ProcessFailure.MissingPower;
                break;
            }

            int removed = 0;
            foreach (var run in runs)
            {
                int amount = WaterHelper.RemoveContaminantFromContainer(water, run.contaminant, run.amount, 0);
                if (amount != run.amount)
                {
                    rollbackOutputs();
                    failure = ProcessFailure.MissingWater;
                    break;
                }
                removed += amount;
            }
            if (failure != ProcessFailure.None) break;
            WaterHelper.AddLiquid(water, "water", removed);
            WaterFeatureHelper.UpdateWaterFeature(water, isExpose: false);
            SpriteHelper.UpdateWaterContainerSprite(water);
            if (definition.moduleBay) MachineHelper.OnMachineActioned(machine, moduleGrid);
            batches += runs.Sum(run => run.batches);
        }
        WriteStatus(machine, batches, cycles <= 0 ? ProcessFailure.NoCapacity : failure);
        return batches > 0;
    }

    private static ProcessFailure MoreActionable(ProcessFailure current, ProcessFailure candidate)
    {
        static int Priority(ProcessFailure failure) => failure switch
        {
            ProcessFailure.MissingInputs => 1,
            ProcessFailure.MissingWater => 2,
            ProcessFailure.MissingPower => 3,
            ProcessFailure.OutputSpace => 4,
            ProcessFailure.OutputUnavailable => 5,
            ProcessFailure.NoCapacity => 6,
            _ => 0,
        };
        return Priority(candidate) > Priority(current) ? candidate : current;
    }

    private static ProcessFailure TryRunRecipe(
        GameItem machine,
        MachineDefinition machineDefinition,
        RecipeDefinition recipe,
        GameSlotInventory batterySlot,
        GameSlotInventory waterSlot,
        GameGridInventory inputGrid,
        GameGridInventory outputGrid,
        GameGridInventory moduleGrid)
    {
        if (recipe.valueRoll != null)
        {
            return ModuleAssembly.TryRun(machine, machineDefinition, recipe, batterySlot, inputGrid, outputGrid, moduleGrid);
        }
        List<GameItem> pool = MachineFactory.Items(inputGrid);
        var consumed = new Dictionary<GameItem, int>();
        var wear = new Dictionary<GameItem, int>();
        if (!TryMatchInputs(recipe, pool, consumed, wear))
        {
            return ProcessFailure.MissingInputs;
        }

        bool electrolysis = machineDefinition.id == Ids.ElectrolysisCell && recipe.id == "elec_water";
        int performance = electrolysis ? Math.Clamp(MachineryHelper.GetCurrentPerformanceBonus(machine), -100, 6300) : 0;
        int throughput = 100 + performance;
        if (electrolysis && throughput <= 0) return ProcessFailure.NoCapacity;
        int waterRequired = electrolysis ? (int)((long)recipe.water * throughput / 100) : recipe.water;
        GameItem? water = waterSlot.childItem;
        if (waterRequired > 0)
        {
            if (water == null || water.unitCount != 1)
            {
                return ProcessFailure.MissingWater;
            }
            if (!HasWaterAmount(water, recipe, waterRequired))
            {
                return ProcessFailure.MissingWater;
            }
        }

        int power = RecipePower(machine, machineDefinition, recipe);
        GameItem? battery = batterySlot.childItem;
        if (!CanDraw(battery, power))
        {
            return ProcessFailure.MissingPower;
        }

        var outputs = new List<GameItem>();
        foreach (OutputAmountDefinition output in recipe.outputs)
        {
            int outputCount = output.count;
            if (electrolysis)
            {
                outputCount = ScaleAmount(outputCount, throughput);
                if (output.id == Ids.HydrogenCylinder)
                {
                    int quality = Math.Clamp(MachineryHelper.GetCurrentQualityBonus(machine), -100, 300);
                    outputCount = ScaleAmount(outputCount, 100 + quality);
                }
                if (outputCount <= 0) continue;
            }
            if (ContentDatabase.Items.TryGetValue(output.id, out ItemDefinition? itemDefinition) &&
                !string.IsNullOrEmpty(itemDefinition.gas))
            {
                GameItem? gas = TryCreateOutput(output, machine, applyProductQuality: !electrolysis);
                if (gas == null)
                {
                    MachineFactory.DestroyAll(outputs);
                    return ProcessFailure.OutputUnavailable;
                }
                gas.SetUnitCount(outputCount);
                ProductQuality.RefreshValue(gas);
                outputs.Add(gas);
                continue;
            }
            for (int i = 0; i < outputCount; i++)
            {
                GameItem? produced = TryCreateOutput(output, machine);
                if (produced == null)
                {
                    MachineFactory.DestroyAll(outputs);
                    return ProcessFailure.OutputUnavailable;
                }
                outputs.Add(produced);
            }
        }
        if (!MachineFactory.TryPlaceOutputs(outputGrid, outputs, out Action rollbackOutputs))
        {
            return ProcessFailure.OutputSpace;
        }
        if (power > 0 && (battery == null || battery.unitCount != 1 || !PowerHelper.DrawPowerSource(battery, power)))
        {
            rollbackOutputs();
            return ProcessFailure.MissingPower;
        }

        ConsumeInputs(consumed);
        foreach (KeyValuePair<GameItem, int> entry in wear) Wear(entry.Key, entry.Value);
        ConsumeWater(water, recipe, waterRequired);

        if (machineDefinition.moduleBay)
        {
            MachineHelper.OnMachineActioned(machine, moduleGrid);
        }
        return ProcessFailure.None;
    }

    /// <summary>Recipe power scaled by the machine's current draw, so efficiency modules apply.</summary>
    internal static int RecipePower(GameItem machine, MachineDefinition machineDefinition, RecipeDefinition recipe)
    {
        int basePower = Math.Max(1, machineDefinition.power);
        int recipePower = recipe.power > 0 ? recipe.power : basePower;
        int currentPower = Math.Max(1, MachineryHelper.GetMachinePowerUsage(machine));
        return Math.Max(1, recipePower * currentPower / basePower);
    }

    internal static bool CanDraw(GameItem? battery, int power)
        => power <= 0 || (battery != null && battery.unitCount == 1 && PowerHelper.CanDrawPowerSource(battery, power));

    /// <summary>Spends durability; a tool worn down to zero is used up.</summary>
    internal static void Wear(GameItem item, int amount)
    {
        DurabilityHelper.ChangeDurability(item, -amount);
        if (DurabilityHelper.GetRealDurability(item) <= 0) item.Destroy();
    }

    // Gas stacks use whole grams. Round each scaled output to the nearest gram.
    private static int ScaleAmount(int amount, int percent)
        => (int)Math.Min(int.MaxValue, ((long)amount * percent + 50) / 100);

    /// <summary>
    /// Matches recipe inputs against the grid pool. A stack counts as its unit count, so
    /// players no longer have to split bottles and ore stacks by hand before every run.
    /// </summary>
    private static bool TryMatchInputs(
        RecipeDefinition recipe,
        List<GameItem> pool,
        Dictionary<GameItem, int> consumed,
        Dictionary<GameItem, int> wear)
    {
        foreach (ItemAmountDefinition input in recipe.inputs)
        {
            if (input.durability > 0)
            {
                // A tool input wears one item instead of consuming units from the pool.
                GameItem? tool = pool.FirstOrDefault(item => !consumed.ContainsKey(item) && !wear.ContainsKey(item) &&
                    Matches(input, item) && DurabilityHelper.GetRealDurability(item) >= input.durability);
                if (tool == null) return false;
                wear[tool] = input.durability;
                continue;
            }
            int needed = Math.Max(1, input.count);
            foreach (GameItem item in pool)
            {
                if (needed <= 0)
                {
                    break;
                }
                int alreadyUsed = consumed.TryGetValue(item, out int used) ? used : 0;
                int available = item.unitCount - alreadyUsed;
                if (available <= 0 || wear.ContainsKey(item) || !Matches(input, item))
                {
                    continue;
                }
                int take = Math.Min(available, needed);
                consumed[item] = alreadyUsed + take;
                needed -= take;
            }
            if (needed > 0)
            {
                return false;
            }
        }
        return true;
    }

    private static void ConsumeInputs(Dictionary<GameItem, int> consumed)
    {
        foreach (KeyValuePair<GameItem, int> entry in consumed)
        {
            if (entry.Value >= entry.Key.unitCount)
            {
                entry.Key.Destroy();
            }
            else if (GasContainer.IsGasContainer(entry.Key))
            {
                GasContainer.Consume(entry.Key, entry.Value);
            }
            else
            {
                entry.Key.SetUnitCount(entry.Key.unitCount - entry.Value);
            }
        }
    }

    private static bool Matches(ItemAmountDefinition input, GameItem item)
    {
        if (item == null || item.unitCount <= 0)
        {
            return false;
        }
        if (!string.IsNullOrEmpty(input.id))
        {
            if (!string.Equals(input.id, item.identifier, StringComparison.Ordinal))
            {
                return false;
            }
        }
        else if (string.IsNullOrEmpty(input.tag) || !item.IsTag(input.tag))
        {
            return false;
        }

        // A recipe may ask for one grade of a substance (industrial vs electronic
        // silicon); the same item at another purity must not satisfy it.
        if (!string.IsNullOrEmpty(input.purity))
        {
            if (!item.IsTag("INGOT_PURITY_TAG")) return false;
            string actual = IngotPurityHelper.GetPurity(item);
            if ((input.minimumPurity || input.maximumPurity) && IngotPurityHelper.ingotPurities.TryGetValue(actual, out var actualGrade) &&
                IngotPurityHelper.ingotPurities.TryGetValue(input.purity, out var requiredGrade))
            {
                if ((input.minimumPurity && actualGrade.PurityLevel < requiredGrade.PurityLevel) ||
                    (input.maximumPurity && actualGrade.PurityLevel > requiredGrade.PurityLevel)) return false;
            }
            else if (!string.Equals(actual, input.purity, StringComparison.Ordinal)) return false;
        }
        return true;
    }

    private static bool HasWaterAmount(GameItem water, RecipeDefinition recipe, int waterRequired)
    {
        if (!string.IsNullOrEmpty(recipe.waterContaminant))
        {
            return WaterHelper.GetContaminantAmount(water, recipe.waterContaminant) >= waterRequired;
        }
        // Total liquid also includes acid, alcohol and contaminants. Only the
        // actual water component can satisfy a water recipe.
        return water.GetTagReadonly(Liquid.GetCurrentPartFromId("water")).GetInt() >= waterRequired;
    }

    private static void ConsumeWater(GameItem? water, RecipeDefinition recipe, int waterRequired)
    {
        if (water == null || waterRequired <= 0)
        {
            return;
        }
        if (!string.IsNullOrEmpty(recipe.waterContaminant))
        {
            int removed = WaterHelper.RemoveContaminantFromContainer(water, recipe.waterContaminant, waterRequired, 0);
            if (removed > 0)
            {
                WaterHelper.AddLiquid(water, "water", removed);
            }
        }
        else
        {
            string waterTag = Liquid.GetCurrentPartFromId("water");
            int remaining = water.GetTagReadonly(waterTag).GetInt() - waterRequired;
            water.ModifyTag(waterTag, state => state.Enable().SetInt(remaining));
            WaterFeatureHelper.OnLiquidContainerPourOutOf(water);
        }
        WaterFeatureHelper.UpdateWaterFeature(water, isExpose: false);
        SpriteHelper.UpdateWaterContainerSprite(water);
    }

    /// <summary>Weighted pick; RNG.GetRandomInt is inclusive on both ends.</summary>
    internal static RollEntryDefinition Roll(RollEntryDefinition[] pool)
    {
        int ticket = RNG.GetRandomInt(1, pool.Sum(entry => entry.weight));
        foreach (RollEntryDefinition entry in pool)
        {
            ticket -= entry.weight;
            if (ticket <= 0) return entry;
        }
        return pool[pool.Length - 1];
    }

    private static GameItem? TryCreateOutput(OutputAmountDefinition output, GameItem machine, bool applyProductQuality = true)
    {
        if (output.roll.Length > 0)
        {
            // Native random_* keys go through the prebuilt table, which rolls the module
            // shape and effects; plain ids fall through to DirectoryMaster.
            GameItem rolled = ItemSpawner.Spawn(Roll(output.roll).spawn);
            if (rolled == null) return null;
            if (rolled.spriteAtlasPath == "Items/itematlas" && rolled.spritePath == "unknown")
            {
                rolled.Destroy();
                return null;
            }
            return rolled;
        }
        // DirectoryMaster assigns ownership and a unique id. Creating mod items via
        // ItemFactory directly bypasses both, making outputs unusable in owned-only slots.
        GameItem item = DirectoryMaster.Item(output.id);
        if (item == null)
        {
            return null;
        }
        if (item.spriteAtlasPath == "Items/itematlas" && item.spritePath == "unknown")
        {
            item.Destroy();
            return null;
        }
        if (item.IsTag("INGOT_PURITY_TAG") || output.id == "metal_ingot")
        {
            if (!item.IsTag("INGOT_PURITY_TAG"))
            {
                IngotPurityHelper.InitIngotItem(item);
            }
            if (!string.IsNullOrEmpty(output.purity))
            {
                int qualitySteps = MachineryHelper.GetCurrentQualityBonus(machine) / 100;
                IngotPurityHelper.SetPurity(item, IngotPurityHelper.GetPurityConst(output.purity, qualitySteps));
            }
        }
        else if (applyProductQuality)
        {
            ProductQuality.Apply(item, MachineryHelper.GetCurrentQualityBonus(machine));
        }
        return item;
    }
}
