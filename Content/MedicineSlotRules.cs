using System;
using System.Collections.Generic;
using System.Linq;

namespace ElementalIndustry;

internal static class MedicineSlotRules
{
    private static readonly HashSet<string> MedicalTags = new(StringComparer.Ordinal)
    {
        "MEDICAL", "MEDICAL_SUPPLIES", "CHEMICAL_SUPPLIES", "NARCOTIC",
        "CHEMICAL_PRODUCT_TAG", "LIQUID_CONTAINER_TAG", "WATER_PURIFICATION_SUPPLY"
    };

    internal static bool IsAccepted(IEnumerable<string> tags) => tags.Any(MedicalTags.Contains);

    internal static bool IsModStackable(ItemDefinition definition) =>
        definition.itemTypes.Contains("CHEMICAL_SUPPLIES") ||
        definition.itemTypes.Contains("MEDICAL_SUPPLIES") ||
        definition.itemTypes.Contains("WATER_PURIFICATION_SUPPLY");

    internal static bool CanShareSlot(string? currentId, string incomingId, bool stackable,
        bool itemPairMayStack, int currentCount, int maximum)
    {
        if (string.IsNullOrEmpty(currentId)) return true;
        return stackable && itemPairMayStack && currentId == incomingId && currentCount < maximum;
    }

    internal static bool MayStack(string leftId, string rightId, bool stackable,
        bool existingItemIsInLaboratorySlot) =>
        stackable && leftId == rightId && existingItemIsInLaboratorySlot;
}
