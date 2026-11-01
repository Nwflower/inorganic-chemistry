using System.Linq;

namespace ElementalIndustry;

internal static class SaltExtraction
{
    private const int ExtractedVolume = 1000;

    internal static void RegisterVanillaChemicalFinisherRecipe()
    {
        if (!ContentDatabase.IsContentEnabled("ChemicalProcessing") ||
            !ContentDatabase.Items.TryGetValue(Ids.Salt, out ItemDefinition? salt) ||
            string.IsNullOrEmpty(salt.chemicalProductIngredient) ||
            ChemFinisherRecipe.chemFinisherRecipes.Any(recipe => recipe.identifier == Ids.Salt))
        {
            return;
        }

        ChemFinisherRecipe.chemFinisherRecipes.Add(new ChemFinisherRecipe
        {
            identifier = Ids.Salt,
            displayName = Loc.Get("item." + Ids.Salt + ".name"),
            requiredVolume = ExtractedVolume,
            isRequireDry = false,
            activeIngredientId = salt.chemicalProductIngredient,
        });
    }
}
