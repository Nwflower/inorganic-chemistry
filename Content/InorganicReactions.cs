using System;
using System.Collections.Generic;

namespace ElementalIndustry;

/// <summary>Gameplay-volume equivalents, not molar concentrations or real reaction rates.</summary>
internal static class InorganicReactions
{
    internal const string Hcl = "elem_hydrochloric_acid";
    internal const string Nitric = "elem_nitric_acid";
    internal const string Caustic = "elem_sodium_hydroxide";
    internal const string Ammonia = "elem_ammonia";
    internal const string Nitrate = "elem_ammonium_nitrate";
    internal const string Peroxide = "elem_hydrogen_peroxide";
    internal const string Hypochlorite = "elem_hypochlorite";
    internal static readonly string[] Acids = { Hcl, Nitric };
    internal static readonly string[] Bases = { Caustic, Ammonia };

    internal static int Amount(IDictionary<string, int> parts, string id)
        => parts.TryGetValue(id, out int value) ? Math.Max(0, value) : 0;

    private static void Add(IDictionary<string, int> parts, string id, int amount)
        => parts[id] = checked(Amount(parts, id) + amount);

    private static int TakePair(IDictionary<string, int> parts, string a, string b)
    {
        int amount = Math.Min(Amount(parts, a), Amount(parts, b));
        if (amount == 0) return 0;
        Add(parts, a, -amount);
        Add(parts, b, -amount);
        return amount;
    }

    internal static void Tick(IDictionary<string, int> parts)
    {
        // Specific chemistry precedes generic neutralization: nitric acid + ammonia
        // makes nitrogen fertilizer; hydrochloric acid must never make nitrate.
        int nitrate = TakePair(parts, Nitric, Ammonia);
        Add(parts, Nitrate, checked(nitrate * 2));
        foreach (string acid in new[] { Hcl, Nitric, "acid" })
        {
            // Acidifying bleach destroys the useful reagent and creates a pollution
            // penalty. Escaping chlorine is abstracted; there is no world gas simulation.
            int spoiled = TakePair(parts, acid, Hypochlorite);
            Add(parts, "chemical_contaminant", spoiled);
            Add(parts, "water", spoiled);
            foreach (string alkali in new[] { Caustic, Ammonia, "base" })
            {
                if (acid == "acid" && alkali == "base") continue; // Vanilla owns this pair.
                int neutralized = TakePair(parts, acid, alkali);
                Add(parts, "mineral", neutralized);
                Add(parts, "water", neutralized);
            }
        }
        if (Amount(parts, "water") <= 0) return;

        // Diluted peroxide disinfects and decomposes into water. Native chlorine
        // handles hypochlorite's residual disinfectant effect on this same tick.
        int peroxide = Math.Min(Amount(parts, Peroxide), 1000);
        if (peroxide > 0)
        {
            Add(parts, Peroxide, -peroxide);
            Add(parts, "water", peroxide);
            foreach (string contaminant in new[] { "microbe", "chemical_contaminant" })
            {
                int removed = Math.Min(Amount(parts, contaminant), peroxide * (contaminant == "microbe" ? 5 : 1));
                Add(parts, contaminant, -removed);
                Add(parts, "water", removed);
            }
        }
        int chlorine = Math.Min(Amount(parts, Hypochlorite), 15000);
        Add(parts, Hypochlorite, -chlorine);
        Add(parts, "chlorine", chlorine);
    }
}
