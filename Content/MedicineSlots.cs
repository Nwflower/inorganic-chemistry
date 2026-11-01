using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ElementalIndustry;

internal static class MedicineSlots
{
    internal const string LaboratorySlotPrefix = "elem_laboratory_bay_medicine_slot_";
    internal const int SlotCount = 12;
    internal const int ModStackLimit = 99;

    internal static bool IsModStackableMedicine(ItemDefinition definition) =>
        MedicineSlotRules.IsModStackable(definition);

    internal static bool IsModStackableMedicine(GameItem item)
    {
        return item != null && ContentDatabase.Items.TryGetValue(item.identifier, out ItemDefinition? definition) &&
               MedicineSlotRules.IsModStackable(definition);
    }

    internal static (List<MachineryPanel> Panels, List<GameSlotInventory> Slots) CreatePanels(
        GameItem owner, string slotPrefix)
    {
        var panels = new List<MachineryPanel>();
        var slots = new List<GameSlotInventory>();
        for (int index = 0; index < SlotCount; index++)
        {
            int slotIndex = index;
            var slot = new GameSlotInventory(2, 2)
            {
                identifier = slotPrefix + slotIndex
            };
            slot.SetBackgroundFadeSprite("Items/items_medical", "pill_bottle");
            ContainerHelper.InitContainerItem(slot, owner, new List<string>
            {
                "MEDICAL", "MEDICAL_SUPPLIES", "CHEMICAL_SUPPLIES", "NARCOTIC",
                "CHEMICAL_PRODUCT_TAG", "LIQUID_CONTAINER_TAG", "WATER_PURIFICATION_SUPPLY"
            });
            Func<GameItem, GameInventory, bool>? vanillaFilter = slot.mayInventoryAddItemFunc;
            slot.mayInventoryAddItemFunc = (item, inventory) =>
                vanillaFilter?.Invoke(item, inventory) == true && CanStore(slot, item);
            slot.maxSlotAmountItemFunc = (item, _, _) => IsModStackableMedicine(item) ? ModStackLimit : 1;
            panels.Add(MachineHelper.Panel(Loc.Get("laboratory.medicine_slot", slotIndex + 1), slot, "medicine" + slotIndex));
            slots.Add(slot);
        }
        return (panels, slots);
    }

    private static bool CanStore(GameSlotInventory slot, GameItem item)
    {
        if (item == null || item.IsGameItemType("MACHINE") || item.IsTag("BACKPACK_TAG") ||
            item.IsTag("CONTAINER_TAG") || !MedicineSlotRules.IsAccepted(item.itemTypes.Concat(
                new[] { "MEDICAL", "MEDICAL_SUPPLIES", "CHEMICAL_SUPPLIES", "NARCOTIC", "CHEMICAL_PRODUCT_TAG", "LIQUID_CONTAINER_TAG", "WATER_PURIFICATION_SUPPLY" }
                    .Where(item.IsTag))))
        {
            return false;
        }

        GameItem? current = slot.childItem;
        return MedicineSlotRules.CanShareSlot(current?.identifier, item.identifier,
            IsModStackableMedicine(item), current != null && current.MayStack(item),
            current?.unitCount ?? 0, ModStackLimit);
    }

    internal static bool MayStackInLaboratorySlot(GameItem item, GameItem other)
    {
        return MedicineSlotRules.MayStack(item.identifier, other.identifier,
            IsModStackableMedicine(item), IsLaboratoryMedicineSlot(item.parentInventory));
    }

    private static bool IsLaboratoryMedicineSlot(GameInventory? inventory)
    {
        string? identifier = inventory?.identifier;
        return identifier?.StartsWith(LaboratorySlotPrefix, StringComparison.Ordinal) == true;
    }
}
