using System;
using UnityEngine;

namespace ElementalIndustry;

/// <summary>
/// Public extension surface for optional third-party content packs. The built-in content
/// uses JSON, but the same registries are exposed here so another assembly can add a
/// module/item/machine/recipe before the item directory is initialized.
/// </summary>
public static class ElementalIndustryApi
{
    public static bool RegisterModule(ModuleDefinition definition)
    {
        if (definition == null || string.IsNullOrWhiteSpace(definition.id))
        {
            return false;
        }
        if (ContentDatabase.Modules.ContainsKey(definition.id))
        {
            return false;
        }
        ContentDatabase.Modules.Add(definition.id, definition);
        return true;
    }

    public static bool RegisterItem(ItemDefinition definition)
    {
        if (definition == null || string.IsNullOrWhiteSpace(definition.id) ||
            !ContentDatabase.Modules.ContainsKey(definition.module))
        {
            return false;
        }
        if (ContentDatabase.Items.ContainsKey(definition.id))
        {
            return false;
        }
        ContentDatabase.Items.Add(definition.id, definition);
        return true;
    }

    public static bool RegisterMachine(MachineDefinition definition)
    {
        if (definition == null || string.IsNullOrWhiteSpace(definition.id) ||
            !ContentDatabase.Modules.ContainsKey(definition.module))
        {
            return false;
        }
        if (ContentDatabase.Machines.ContainsKey(definition.id))
        {
            return false;
        }
        ContentDatabase.Machines.Add(definition.id, definition);
        return true;
    }

    public static bool RegisterRecipe(RecipeDefinition definition)
    {
        if (definition == null || string.IsNullOrWhiteSpace(definition.id) ||
            !ContentDatabase.Machines.ContainsKey(definition.machine))
        {
            return false;
        }
        if (ContentDatabase.Recipes.ContainsKey(definition.id))
        {
            return false;
        }
        ContentDatabase.Recipes.Add(definition.id, definition);
        MachineDefinition machine = ContentDatabase.Machines[definition.machine];
        if (Array.IndexOf(machine.recipeIds, definition.id) < 0)
        {
            var ids = new string[machine.recipeIds.Length + 1];
            Array.Copy(machine.recipeIds, ids, machine.recipeIds.Length);
            ids[ids.Length - 1] = definition.id;
            machine.recipeIds = ids;
        }
        return true;
    }

    public static bool RegisterReaction(RecipeDefinition definition)
    {
        return RegisterRecipe(definition);
    }

    public static bool IsModuleEnabled(string moduleId)
    {
        return ContentDatabase.IsModuleEnabled(moduleId);
    }

    public static bool IsBuildingEnabled(string buildingId)
    {
        return ContentDatabase.IsBuildingEnabled(buildingId);
    }

    public static GameItem CreateItem(string id)
    {
        return ItemFactory.Create(id);
    }

    public static bool IsGasContainer(GameItem? item)
    {
        return GasContainer.IsGasContainer(item);
    }

    public static string GetGasType(GameItem? item)
    {
        return GasContainer.GetGasType(item);
    }

    public static bool IsGas(GameItem? item, string gasType)
    {
        return GasContainer.Is(item, gasType);
    }

    /// <summary>Consumes grams from a gas stack.</summary>
    public static void ConsumeGas(GameItem item, int count = 1)
    {
        GasContainer.Consume(item, count);
    }

    public static void MarkSource(GameItem item)
    {
        if (item == null)
        {
            return;
        }
        ModHelper.SetSource(item, Ids.ModId, Loc.Get("mod.name"));
    }
}
