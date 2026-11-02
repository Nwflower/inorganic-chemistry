using System;
using System.Collections.Generic;
using System.Linq;

namespace ElementalIndustry;

/// <summary>
/// Value-roll assembly: every accepted material in the input grid is spent on one native
/// module whose base value falls between the floor and ceiling of
/// <see cref="ValueRollDefinition"/>. Surplus value buys a premium effect and higher base
/// stats; a shortfall rolls the next module up and risks native defects.
/// </summary>
internal static class ModuleAssembly
{
    // Mirrors of native ModuleEffectHelper / ModuleDirectory identifiers and constants.
    internal const string PremiumEffect = "MODULE_EFFECT_PREMIUM";
    internal const int PremiumValuePerTier = 40;
    internal const string RuinedModule = "system_module_ruined";
    internal const string CorruptModule = "system_module_corrupt";
    internal static readonly string[] NegativeEffects =
    {
        "MODULE_EFFECT_INFERIOR", "MODULE_EFFECT_OVERVOLTED", "MODULE_EFFECT_NEGATIVE_FEEDBACK"
    };

    internal enum Fit { InRange, Surplus, Shortfall }

    internal sealed class Plan
    {
        public List<GameItem> used = new();
        public RollEntryDefinition[] candidates = Array.Empty<RollEntryDefinition>();
        public Fit fit;
        /// <summary>Surplus or shortfall in value; zero when the roll is in range.</summary>
        public long gap;
    }

    internal static bool IsMaterial(ValueRollDefinition rule, GameItem item)
        => item != null && item.unitCount > 0 && rule.materials.Contains(item.identifier);

    internal static bool IsPenaltyOutput(GameItem item)
        => item.identifier == RuinedModule || item.identifier == CorruptModule;

    internal static long CompensationCapacity(ValueRollDefinition rule, long moduleValue)
        => PremiumValuePerTier + moduleValue * rule.maxStatBonus / 100;

    /// <summary>
    /// Chooses inputs and candidates. Inputs that would push the floor past every module are
    /// left in the grid, most valuable first, but the last key material is never left out.
    /// </summary>
    internal static Plan? Choose(ValueRollDefinition rule, RollEntryDefinition[] pool, IList<(GameItem item, long value)> inputs)
    {
        var used = inputs.ToList();
        while (true)
        {
            int keys = used.Count(entry => entry.item.identifier == rule.key);
            if (keys == 0 || pool.Length == 0) return null;
            long keyValue = used.Where(entry => entry.item.identifier == rule.key).Sum(entry => entry.value);
            long otherValue = used.Sum(entry => entry.value) - keyValue;
            long floor = keyValue + otherValue + rule.floorBonus;
            long ceiling = keyValue * rule.keyMultiplier + otherValue * rule.otherMultiplier + rule.ceilingBonus;

            RollEntryDefinition[] inRange = pool.Where(entry => entry.value >= floor && entry.value <= ceiling).ToArray();
            if (inRange.Length > 0) return Result(used, inRange, Fit.InRange, 0);

            long? below = pool.Where(entry => entry.value < floor).Select(entry => (long?)entry.value).Max();
            long? above = pool.Where(entry => entry.value > ceiling).Select(entry => (long?)entry.value).Min();
            // Between two modules, go to whichever is nearer; ties keep the inputs' value.
            if (below.HasValue && (!above.HasValue || floor - below.Value <= above.Value - ceiling))
            {
                long surplus = floor - below.Value;
                RollEntryDefinition[] top = pool.Where(entry => entry.value == below.Value).ToArray();
                if (surplus <= CompensationCapacity(rule, below.Value)) return Result(used, top, Fit.Surplus, surplus);
                (GameItem item, long value)? drop = used
                    .Where(entry => entry.item.identifier != rule.key || keys > 1)
                    .OrderByDescending(entry => entry.value)
                    .Select(entry => ((GameItem, long)?)entry)
                    .FirstOrDefault();
                if (drop == null) return Result(used, top, Fit.Surplus, CompensationCapacity(rule, below.Value));
                used.Remove(drop.Value);
                continue;
            }
            return Result(used, pool.Where(entry => entry.value == above!.Value).ToArray(), Fit.Shortfall, above!.Value - ceiling);
        }
    }

    private static Plan Result(List<(GameItem item, long value)> used, RollEntryDefinition[] candidates, Fit fit, long gap)
        => new() { used = used.Select(entry => entry.item).ToList(), candidates = candidates, fit = fit, gap = gap };

    internal static ProcessFailure TryRun(
        GameItem machine,
        MachineDefinition definition,
        RecipeDefinition recipe,
        GameSlotInventory batterySlot,
        GameGridInventory inputGrid,
        GameGridInventory outputGrid,
        GameGridInventory moduleGrid)
    {
        ValueRollDefinition rule = recipe.valueRoll!;
        RollEntryDefinition[] pool = recipe.outputs.SelectMany(output => output.roll).ToArray();
        var inputs = MachineFactory.Items(inputGrid)
            .Where(item => IsMaterial(rule, item))
            .Select(item => (item, Math.Max(0, item.GetBaseValue())))
            .ToList();
        Plan? plan = Choose(rule, pool, inputs);
        if (plan == null) return ProcessFailure.MissingInputs;

        int power = Processor.RecipePower(machine, definition, recipe);
        GameItem? battery = batterySlot.childItem;
        if (!Processor.CanDraw(battery, power)) return ProcessFailure.MissingPower;

        RollEntryDefinition picked = Processor.Roll(plan.candidates);
        GameItem? module = ItemSpawner.Spawn(picked.spawn);
        if (module == null) return ProcessFailure.OutputUnavailable;
        if (plan.fit == Fit.Surplus) Compensate(module, plan.gap);
        else if (plan.fit == Fit.Shortfall) module = Penalize(module, picked.value, plan.gap);

        if (!MachineFactory.TryPlaceOutputs(outputGrid, new List<GameItem> { module }, out Action rollbackOutputs))
        {
            return ProcessFailure.OutputSpace;
        }
        if (power > 0 && (battery == null || battery.unitCount != 1 || !PowerHelper.DrawPowerSource(battery, power)))
        {
            rollbackOutputs();
            return ProcessFailure.MissingPower;
        }
        foreach (GameItem item in plan.used) item.Destroy();
        if (definition.moduleBay) MachineHelper.OnMachineActioned(machine, moduleGrid);
        return ProcessFailure.None;
    }

    /// <summary>Surplus first buys the native premium effect, the rest raises base stats.</summary>
    internal static void Compensate(GameItem module, long surplus)
    {
        int tier = Math.Max(1, module.GetTagReadonly("MODULE_TIER_INT").GetInt());
        if (surplus >= PremiumValuePerTier * tier)
        {
            ModuleEffectHelper.AddEffectToModule(module, PremiumEffect);
            if (HasEffect(module, PremiumEffect)) surplus -= PremiumValuePerTier * tier;
        }
        long value = Math.Max(1, module.unitValue);
        if (surplus <= 0) return;
        float mult = 1f + (float)surplus / value;
        ModuleHelper.SetBaseStatMult(module, mult, mult);
    }

    /// <summary>
    /// The wider the gap (in percent of the module's value), the likelier and harsher the
    /// defect: ruined, corrupted, shabby, or one extra native negative effect.
    /// </summary>
    internal static GameItem Penalize(GameItem module, long moduleValue, long shortfall)
    {
        int gap = (int)Math.Clamp(shortfall * 100 / Math.Max(1, moduleValue), 1, 99);
        if (RNG.GetRandomInt(1, 100) > Math.Min(100, gap * 2)) return module;
        int severity = RNG.GetRandomInt(1, 100);
        if (severity <= gap) return Replace(module, severity <= gap / 2 ? RuinedModule : CorruptModule);
        if (severity <= 50 + gap / 2) return ItemFeatureHelper.SetShabby(module);
        string effect = NegativeEffects[RNG.GetRandomInt(0, NegativeEffects.Length - 1)];
        ModuleEffectHelper.AddEffectToModule(module, effect);
        // Both effect slots taken or the effect is incompatible: fall back to shabby.
        return HasEffect(module, effect) ? module : ItemFeatureHelper.SetShabby(module);
    }

    private static GameItem Replace(GameItem module, string id)
    {
        GameItem? replacement = ItemSpawner.Spawn(id);
        if (replacement == null) return module;
        module.Destroy();
        return replacement;
    }

    private static bool HasEffect(GameItem module, string effect)
        => module.GetTagReadonly("MODULE_EFFECT1_TAG").GetString() == effect ||
           module.GetTagReadonly("MODULE_EFFECT2_TAG").GetString() == effect;
}
