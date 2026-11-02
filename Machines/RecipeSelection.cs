using System;
using System.Collections.Generic;
using System.Linq;

namespace ElementalIndustry;

/// <summary>Per-machine recipe switches stored as ordinary item tags so they survive saves.</summary>
internal static class RecipeSelection
{
    private const string EnabledPrefix = "ELEM_RECIPE_ENABLED_";
    private const string SelectedTag = "ELEM_SELECTED_RECIPE_ID";

    internal static string Tag(string recipeId)
        => EnabledPrefix + recipeId.ToUpperInvariant();

    internal static bool IsEnabled(GameItem machine, RecipeDefinition recipe)
        => IsEnabled(machine, recipe.id);

    internal static bool IsEnabled(GameItem machine, string recipeId)
    {
        string tag = Tag(recipeId);
        return !machine.IsTag(tag) || machine.GetTagReadonly(tag).GetInt() != 0;
    }

    internal static void SetEnabled(GameItem machine, string recipeId, bool enabled)
    {
        if (string.IsNullOrWhiteSpace(recipeId))
        {
            throw new ArgumentException("Recipe id is required.", nameof(recipeId));
        }
        machine.ModifyTag(Tag(recipeId), state => state.Enable().SetInt(enabled ? 1 : 0));
    }

    internal static string Selected(GameItem machine, IEnumerable<RecipeDefinition> recipes)
    {
        string selected = machine.GetTagReadonly(SelectedTag).GetString();
        return recipes.Any(recipe => recipe.id == selected) ? selected : recipes.First().id;
    }

    internal static void Select(GameItem machine, string recipeId)
        => machine.ModifyTag(SelectedTag, state => state.Enable().SetString(recipeId));
}
