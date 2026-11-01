using System;
using System.Collections.Generic;
using System.Linq;

namespace ElementalIndustry;

internal enum ChemistOfferKind { Directory, AcidBottle, BaseBottle, ReducingBottle }

internal readonly struct ChemistOffer
{
    public readonly string Key;
    public readonly string Identifier;
    public readonly ChemistOfferKind Kind;
    // Larger shapes go first so small bottles cannot fragment the counter around them.
    public readonly int Priority;

    public ChemistOffer(string key, string identifier, ChemistOfferKind kind = ChemistOfferKind.Directory,
        int priority = 1)
    {
        Key = key;
        Identifier = identifier;
        Kind = kind;
        Priority = priority;
    }
}

/// <summary>Pure, deterministic assortment. No game objects are created here.</summary>
internal static class ChemistStockPlan
{
    // Vanilla chemistry appliances the retired chemist already sells.
    private static readonly string[] VanillaMachines =
        { "blender", "heating_plate", "evaporator", "chem_finisher", "chem_scanner", "chem_module" };

    // Vanilla medical supplies; no new effects are claimed, these are the game's own items.
    private static readonly string[] Medicine =
        { "bandage_item", "topical_bandage_item", "salve", "phagimycin_pill", "common_chemical", "restricted_chemical" };

    internal static List<ChemistOffer> Build(IEnumerable<string> enabledMachines, IEnumerable<string> enabledChemicals)
    {
        var offers = new List<ChemistOffer>
        {
            // Storage hardware is offered before small goods to preserve a large footprint.
            new("id:elem_laboratory_bay", "elem_laboratory_bay", priority: 16)
        };
        offers.AddRange(VanillaMachines.Select(id => new ChemistOffer("id:" + id, id, priority: 4)));
        offers.AddRange(DirectoryOffers(enabledMachines, 4));
        offers.Add(new("bottle:acid", "med_bottle_red", ChemistOfferKind.AcidBottle, priority: 2));
        offers.Add(new("bottle:base", "med_bottle_blue", ChemistOfferKind.BaseBottle, priority: 2));
        offers.Add(new("bottle:reducing", "med_bottle_orange", ChemistOfferKind.ReducingBottle, priority: 2));
        offers.AddRange(DirectoryOffers(enabledChemicals, 1));
        offers.AddRange(DirectoryOffers(Medicine, 1));

        var seen = new HashSet<string>(StringComparer.Ordinal);
        return offers
            .Where(offer => seen.Add(offer.Key))
            .OrderByDescending(offer => offer.Priority)
            .ThenBy(offer => offer.Key, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Prioritises large goods, then rotates the remaining order by game day so the
    /// counter never shows every item in the same layout. All candidates stay in the
    /// plan; the game skips only the ones that genuinely do not fit that day.
    /// </summary>
    internal static List<ChemistOffer> ForDay(IReadOnlyList<ChemistOffer> candidates, int day)
    {
        var ordered = candidates.OrderByDescending(offer => offer.Priority)
            .ThenBy(offer => offer.Key, StringComparer.Ordinal).ToList();
        int firstSmall = ordered.FindIndex(offer => offer.Priority < 4);
        if (firstSmall < 0 || ordered.Count - firstSmall < 2)
        {
            return ordered;
        }
        var head = ordered.Take(firstSmall).ToList();
        var tail = ordered.Skip(firstSmall).ToList();
        int offset = Math.Max(0, day) % tail.Count;
        head.AddRange(tail.Skip(offset).Concat(tail.Take(offset)));
        return head;
    }

    private static IEnumerable<ChemistOffer> DirectoryOffers(IEnumerable<string> identifiers, int priority = 1)
    {
        return identifiers.Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal).OrderBy(id => id, StringComparer.Ordinal)
            .Select(id => new ChemistOffer("id:" + id, id, priority: priority));
    }
}
