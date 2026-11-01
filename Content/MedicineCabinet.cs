using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ElementalIndustry;

internal static class MedicineCabinet
{
    internal const string ItemId = "elem_medicine_cabinet";
    internal const string SlotPrefix = "elem_medicine_cabinet_slot_";
    internal const string LaboratorySlotPrefix = "elem_laboratory_bay_medicine_slot_";
    internal const int SlotCount = 12;
    internal const int ModStackLimit = 99;

    internal static bool IsModStackableMedicine(ItemDefinition definition) =>
        MedicineCabinetRules.IsModStackable(definition);

    internal static bool IsModStackableMedicine(GameItem item)
    {
        return item != null && ContentDatabase.Items.TryGetValue(item.identifier, out ItemDefinition? definition) &&
               MedicineCabinetRules.IsModStackable(definition);
    }

    internal static GameItem Create(ItemDefinition definition)
    {
        GameItem cabinet = ItemDirectory.CreateEmptyItem(definition.id)
            .SetName(Loc.Get("item." + definition.id + ".name"));
        cabinet.shortDescription = Loc.Get("item." + definition.id + ".desc");
        cabinet.unitValue = definition.value;
        cabinet.EnableTag("CONTAINER_TAG", skipValidate: true);
        cabinet.EnableTag("STORABLE_PACKAGE_TAG", skipValidate: true);
        cabinet.EnableTag("IMPORTANT_TAG", skipValidate: true);
        cabinet.EnableTag("SYSTEM_TAG", skipValidate: true);
        cabinet.EnableTag("SYSTEM_TAG_UTILITY", skipValidate: true);
        cabinet.SetGameItemType("STORAGE");
        cabinet.SetSpriteAndShapeFromMod(Ids.ModId, "elem_medicine_cabinet");
        ModHelper.SetSource(cabinet, Ids.ModId, Ids.ModName);

        var medicine = CreatePanels(cabinet, SlotPrefix);
        PixelWindow window = MachineHelper.CreateMachineryWindow(canMove: true, medicine.Panels.ToArray());
        if (window.childElement is MachineryPanelRow row)
        {
            row.SetColumns(3);
        }
        window.SetTitle(Loc.Get("item." + definition.id + ".name"));
        window.Validate();
        cabinet.SetContentWindow(window);
        return cabinet;
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
            panels.Add(MachineHelper.Panel(Loc.Get("medicine_cabinet.slot", slotIndex + 1), slot, "medicine" + slotIndex));
            slots.Add(slot);
        }
        return (panels, slots);
    }

    private static bool CanStore(GameSlotInventory slot, GameItem item)
    {
        if (item == null || item.IsGameItemType("MACHINE") || item.IsTag("BACKPACK_TAG") ||
            item.IsTag("CONTAINER_TAG") || !MedicineCabinetRules.IsAccepted(item.itemTypes.Concat(
                new[] { "MEDICAL", "MEDICAL_SUPPLIES", "CHEMICAL_SUPPLIES", "NARCOTIC", "CHEMICAL_PRODUCT_TAG", "LIQUID_CONTAINER_TAG", "WATER_PURIFICATION_SUPPLY" }
                    .Where(item.IsTag))))
        {
            return false;
        }

        GameItem? current = slot.childItem;
        return MedicineCabinetRules.CanShareSlot(current?.identifier, item.identifier,
            IsModStackableMedicine(item), current != null && current.MayStack(item),
            current?.unitCount ?? 0, ModStackLimit);
    }

    internal static bool MayStackInCabinet(GameItem item, GameItem other)
    {
        return MedicineCabinetRules.MayStack(item.identifier, other.identifier,
            IsModStackableMedicine(item), IsMedicineSlot(item.parentInventory));
    }

    private static bool IsMedicineSlot(GameInventory? inventory)
    {
        string? identifier = inventory?.identifier;
        return identifier?.StartsWith(SlotPrefix, StringComparison.Ordinal) == true ||
               identifier?.StartsWith(LaboratorySlotPrefix, StringComparison.Ordinal) == true;
    }
}
