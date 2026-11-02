using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace ElementalIndustry;

/// <summary>
/// Loads all content from Data/*.json. The game's JsonUtility is used deliberately:
/// it is available in the target runtime and keeps the mod free of extra assemblies.
/// </summary>
internal static class ContentDatabase
{
    public static readonly Dictionary<string, ModuleDefinition> Modules = new(StringComparer.Ordinal);
    public static readonly Dictionary<string, BuildingDefinition> Buildings = new(StringComparer.Ordinal);
    public static readonly Dictionary<string, ItemDefinition> Items = new(StringComparer.Ordinal);
    public static readonly Dictionary<string, MachineDefinition> Machines = new(StringComparer.Ordinal);
    public static readonly Dictionary<string, RecipeDefinition> Recipes = new(StringComparer.Ordinal);
    public static readonly List<LootPatchDefinition> LootPatches = new();

    public static bool Loaded { get; private set; }

    /// <summary>True when the loader has not registered this mod yet, so retrying is safe.</summary>
    public static bool NotReady { get; private set; }

    public static bool Load()
    {
        if (ModHelper.GetModFolder(Ids.ModId) == null)
        {
            NotReady = true;
            return false;
        }
        NotReady = false;

        Modules.Clear();
        Buildings.Clear();
        Items.Clear();
        Machines.Clear();
        Recipes.Clear();
        LootPatches.Clear();

        int errors = 0;
        ModuleFile? modules = Read<ModuleFile>("Data/modules.json", ref errors);
        if (modules != null)
        {
            foreach (ModuleDefinition module in modules.modules)
            {
                if (!ValidId(module.id))
                {
                    errors++;
                    continue;
                }
                if (!Modules.TryAdd(module.id, module))
                {
                    errors++;
                    Debug.LogError($"[{Ids.ModId}] duplicate module id: {module.id}");
                }
            }
        }

        foreach (ModuleDefinition module in Modules.Values)
        {
            foreach (string dependency in module.dependsOn)
            {
                if (!Modules.ContainsKey(dependency))
                {
                    Debug.LogError($"[{Ids.ModId}] module {module.id} references unknown dependency {dependency}");
                    errors++;
                }
            }
        }

        BuildingFile? buildings = Read<BuildingFile>("Data/buildings.json", ref errors);
        if (buildings != null)
        {
            foreach (BuildingDefinition building in buildings.buildings)
            {
                if (!ValidId(building.id))
                {
                    errors++;
                    continue;
                }
                if (!Buildings.TryAdd(building.id, building))
                {
                    errors++;
                    Debug.LogError($"[{Ids.ModId}] duplicate building id: {building.id}");
                }
            }
        }

        ItemFile? items = Read<ItemFile>("Data/items.json", ref errors);
        if (items != null)
        {
            foreach (ItemDefinition item in items.items)
            {
                if (!ValidId(item.id))
                {
                    errors++;
                    continue;
                }
                if (!Modules.ContainsKey(item.module))
                {
                    Debug.LogError($"[{Ids.ModId}] item {item.id} references unknown module {item.module}");
                    errors++;
                    continue;
                }
                if (!Items.TryAdd(item.id, item))
                {
                    errors++;
                    Debug.LogError($"[{Ids.ModId}] duplicate item id: {item.id}");
                }
            }
        }

        MachineFile? machines = Read<MachineFile>("Data/machines.json", ref errors);
        if (machines != null)
        {
            foreach (MachineDefinition machine in machines.machines)
            {
                if (!ValidId(machine.id))
                {
                    errors++;
                    continue;
                }
                if (!Modules.ContainsKey(machine.module))
                {
                    Debug.LogError($"[{Ids.ModId}] machine {machine.id} references unknown module {machine.module}");
                    errors++;
                    continue;
                }
                if (!Machines.TryAdd(machine.id, machine))
                {
                    errors++;
                    Debug.LogError($"[{Ids.ModId}] duplicate machine id: {machine.id}");
                }
            }
        }

        RecipeFile? recipes = Read<RecipeFile>("Data/recipes.json", ref errors);
        if (recipes != null)
        {
            foreach (RecipeDefinition recipe in recipes.recipes)
            {
                if (!ValidId(recipe.id))
                {
                    errors++;
                    continue;
                }
                if (!Recipes.TryAdd(recipe.id, recipe))
                {
                    errors++;
                    Debug.LogError($"[{Ids.ModId}] duplicate recipe id: {recipe.id}");
                }
            }
        }

        LootPatchFile? loot = Read<LootPatchFile>("Data/loot.json", ref errors);
        if (loot != null)
        {
            LootPatches.AddRange(loot.patches.Where(p => !string.IsNullOrWhiteSpace(p.table)));
        }

        ValidateRecipes(ref errors);
        Loaded = errors == 0;
        Debug.Log($"[{Ids.ModId}] content loaded: {Modules.Count} modules, {Items.Count} items, {Machines.Count} machines, {Recipes.Count} recipes, errors={errors}.");
        return Loaded;
    }

    public static IEnumerable<RecipeDefinition> RecipesFor(string machineId)
    {
        if (machineId == "furnace")
        {
            // Native furnace recipes are enabled only by the installed chemistry module.
            return Recipes.Values.Where(recipe => recipe.machine == machineId && IsRecipeAvailable(recipe))
                .OrderBy(recipe => recipe.id switch
                {
                    "industrial_silicon" => 0,
                    "refine_silicon_fair" => 1,
                    "refine_silicon_high" => 2,
                    "iron_from_oxide_coke" => 3,
                    "iron_from_oxide" => 4,
                    _ => 5
                });
        }
        if (!Machines.TryGetValue(machineId, out MachineDefinition machine))
        {
            return Enumerable.Empty<RecipeDefinition>();
        }
        return machine.recipeIds
            .Select(id => Recipes.TryGetValue(id, out RecipeDefinition recipe) ? recipe : null)
            .Where(recipe => recipe != null && IsRecipeAvailable(recipe))!;
    }

    private static bool IsRecipeAvailable(RecipeDefinition recipe)
    {
        return recipe.inputs.Select(input => input.id).Concat(recipe.outputs.Select(output => output.id))
            .All(id => !Items.TryGetValue(id, out var item) || IsModuleEnabled(item.module));
    }

    public static bool IsModuleEnabled(string moduleId)
    {
        return ModuleSwitches.IsModuleEnabled(moduleId);
    }

    public static bool IsBuildingEnabled(string buildingId)
    {
        return ModuleSwitches.IsBuildingEnabled(buildingId);
    }

    public static bool IsContentEnabled(string moduleId, string buildingId = "")
    {
        return ModuleSwitches.IsContentEnabled(moduleId, buildingId);
    }

    private static T? Read<T>(string relativePath, ref int errors) where T : class
    {
        try
        {
            string? json = ReadContentText(relativePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                Debug.LogError($"[{Ids.ModId}] missing content file: {relativePath}");
                errors++;
                return null;
            }
            // Unity's JsonUtility silently produced empty arrays for these models, so the
            // game's own Json.NET is used; the models only contain public fields.
            T? value = JsonConvert.DeserializeObject<T>(json);
            if (value == null)
            {
                Debug.LogError($"[{Ids.ModId}] invalid content file: {relativePath}");
                errors++;
            }
            return value;
        }
        catch (Exception exception)
        {
            Debug.LogError($"[{Ids.ModId}] cannot load {relativePath}: {exception}");
            errors++;
            return null;
        }
    }

    /// <summary>
    /// The loader registers a mod only after Init/OnEnable, so ModHelper.GetModFolder is
    /// null while this mod is starting. Reading files directly would require the "files"
    /// capability, so content loading is retried from later hooks instead.
    /// </summary>
    private static string? ReadContentText(string relativePath)
    {
        if (ModHelper.GetModFolder(Ids.ModId) == null)
        {
            return null;
        }
        return ModHelper.ReadModFileText(Ids.ModId, relativePath);
    }

    private static void ValidateRecipes(ref int errors)
    {
        foreach (RecipeDefinition recipe in Recipes.Values)
        {
            if (!Machines.ContainsKey(recipe.machine) && recipe.machine != "furnace")
            {
                Debug.LogError($"[{Ids.ModId}] recipe {recipe.id} references unknown machine {recipe.machine}");
                errors++;
            }
            if (recipe.valueRoll != null &&
                (!recipe.valueRoll.materials.Contains(recipe.valueRoll.key) || recipe.outputs.All(output => output.roll.Length == 0)))
            {
                Debug.LogError($"[{Ids.ModId}] recipe {recipe.id} value roll needs its key among the materials and a roll pool");
                errors++;
            }
            foreach (ItemAmountDefinition input in recipe.inputs)
            {
                if (string.IsNullOrEmpty(input.id) && string.IsNullOrEmpty(input.tag))
                {
                    Debug.LogError($"[{Ids.ModId}] recipe {recipe.id} has an input without id/tag");
                    errors++;
                }
                if (!string.IsNullOrEmpty(input.id) && !Items.ContainsKey(input.id) && !IsVanillaItem(input.id))
                {
                    Debug.LogWarning($"[{Ids.ModId}] recipe {recipe.id} input {input.id} is not a mod item; assuming a vanilla/registered id.");
                }
                if (input.count <= 0)
                {
                    Debug.LogError($"[{Ids.ModId}] recipe {recipe.id} input count must be positive");
                    errors++;
                }
            }
            foreach (OutputAmountDefinition output in recipe.outputs)
            {
                if (output.roll.Length > 0)
                {
                    if (output.roll.Any(entry => !ValidId(entry.spawn) || !ValidId(entry.id) || entry.weight <= 0 ||
                            (recipe.valueRoll != null && entry.value <= 0)))
                    {
                        Debug.LogError($"[{Ids.ModId}] recipe {recipe.id} has a roll entry without spawn/id, weight or value");
                        errors++;
                    }
                }
                else if (!Items.ContainsKey(output.id) && !IsVanillaItem(output.id))
                {
                    Debug.LogWarning($"[{Ids.ModId}] recipe {recipe.id} output {output.id} is not a mod item; assuming a vanilla/registered id.");
                }
                if (output.count <= 0)
                {
                    Debug.LogError($"[{Ids.ModId}] recipe {recipe.id} output count must be positive");
                    errors++;
                }
            }
        }
    }

    private static bool IsVanillaItem(string id)
    {
        return id == "metal_ingot" || id == "steel_sheet" || id == "wire" ||
               id == "common_electronic" || id == "blank_module" || id == "scrap_metal" || id == "junk" ||
               id == "flux_agent";
    }

    private static bool ValidId(string id)
    {
        return !string.IsNullOrWhiteSpace(id) && id.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '-');
    }
}
