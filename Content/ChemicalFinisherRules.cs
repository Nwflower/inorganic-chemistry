namespace ElementalIndustry;

/// <summary>Check the composition of the proportional aliquot the native finisher will take.</summary>
internal static class ChemicalFinisherRules
{
    internal static bool CanExtract(ItemDefinition definition, int total, int ingredient, int water, bool wet)
    {
        if (definition.finisherVolume <= 0 || total < definition.finisherVolume ||
            ingredient < 0 || water < 0 || (definition.finisherDry && wet)) return false;
        return (long)ingredient * definition.finisherVolume >= (long)definition.finisherMinIngredient * total &&
               (long)water * definition.finisherVolume >= (long)definition.finisherMinWater * total;
    }
}
