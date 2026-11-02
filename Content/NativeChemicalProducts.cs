using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace ElementalIndustry;

/// <summary>Use the native container, evaporator and finisher UI, including native losses.</summary>
internal static class NativeChemicalProducts
{
    private static readonly Dictionary<string, ItemDefinition> Registered = new();
    private static readonly List<ChemFinisherRecipe> OwnedRecipes = new();
    private static bool _patched;

    internal static void Register(Harmony harmony)
    {
        // Called from the item-directory hook, after localization is available. Do not add
        // HarmonyPatch attributes: the loader would touch ChemFinisherRecipe's static locale data early.
        if (!_patched)
        {
            harmony.Patch(AccessTools.Method(typeof(ChemFinisherRecipe), nameof(ChemFinisherRecipe.CanOutput)),
                postfix: new HarmonyMethod(typeof(NativeChemicalProducts), nameof(CanOutputPostfix)));
            _patched = true;
        }
        foreach (ItemDefinition item in ContentDatabase.Items.Values)
        {
            if (item.finisherVolume <= 0 || !ContentDatabase.IsModuleEnabled(item.module) ||
                Registered.ContainsKey(item.id)) continue;
            if (ChemFinisherRecipe.chemFinisherRecipes.Any(recipe => recipe.identifier == item.id)) continue;
            var recipe = new ChemFinisherRecipe
            {
                identifier = item.id,
                displayName = Loc.Get("item." + item.id + ".name"),
                requiredVolume = item.finisherVolume,
                isRequireDry = item.finisherDry,
                activeIngredientId = item.finisherIngredient,
            };
            Registered.Add(item.id, item);
            OwnedRecipes.Add(recipe);
            ChemFinisherRecipe.chemFinisherRecipes.Add(recipe);
        }
    }

    internal static void Unregister()
    {
        // Remove only instances owned by this registration, leaving other mods' recipes alone.
        foreach (ChemFinisherRecipe recipe in OwnedRecipes)
            ChemFinisherRecipe.chemFinisherRecipes.Remove(recipe);
        OwnedRecipes.Clear();
        Registered.Clear();
        _patched = false;
    }

    private static void CanOutputPostfix(ChemFinisherRecipe __instance, GameItem container, ref bool __result)
    {
        if (!__result || container == null || !Registered.TryGetValue(__instance.identifier, out var item)) return;
        // Vanilla checks only total volume and dryness. Without this gate, plain water could
        // be packaged as an active mod product. Check concentration, not the whole bottle's amount.
        __result = ContentDatabase.IsModuleEnabled(item.module) && ChemicalFinisherRules.CanExtract(item,
            WaterHelper.GetTotalVolume(container),
            container.GetTagReadonly(Liquid.GetCurrentPartFromId(item.finisherIngredient)).GetInt(),
            container.GetTagReadonly(Liquid.GetCurrentPartFromId("water")).GetInt(), WaterHelper.IsWet(container));
    }
}
