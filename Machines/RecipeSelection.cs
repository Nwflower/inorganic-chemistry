using System;

namespace ElementalIndustry;

/// <summary>Per-machine recipe switches stored as ordinary item tags so they survive saves.</summary>
internal static class RecipeSelection
{
    private const string EnabledPrefix = "ELEM_RECIPE_ENABLED_";

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
}
