using System;

namespace ElementalIndustry;

/// <summary>A larger work area built with the same inventory and nesting rules as the vanilla machine bay.</summary>
internal static class LaboratoryBay
{
    internal const string ItemId = "elem_laboratory_bay";
    internal static GameItem Create(ItemDefinition definition)
    {
        (PixelWindow window, GameInventory inventory) = DirectoryUtils.CreateInventoryWindow(14, 18);
        GameItem bay = ItemDirectory.CreateEmptyItem(definition.id).SetContentWindow(window);
        if (!string.IsNullOrEmpty(definition.sprite))
        {
            SetAppearance(bay, definition.sprite, inventory.childItems.Count);
        }
        else
        {
            GameItem template = DirectoryMaster.Item("machine_bay_ext");
            bay.SetSpriteAndShape(template.spriteAtlasPath, template.spritePath);
            template.Destroy();
            SetFootprint(bay);
        }
        bay.SetName(Loc.Get("item." + definition.id + ".name"));
        bay.shortDescription = Loc.Get("item." + definition.id + ".desc");
        bay.unitValue = definition.value;
        ContainerHelper.InitContainerItem(inventory, bay);
        bay.EnableTag("SYSTEM_TAG");
        bay.EnableTag("SYSTEM_TAG_UTILITY");
        bay.SetGameItemType("STORAGE");
        inventory.mayInventoryAddItemFunc = (item, inv) =>
        {
            if (item == null || !GeneralHelper.IsItemOwned(item) || item.IsTag("BACKPACK_TAG")) return false;
            GameItem parent = inv.GetParentItem();
            if (parent != null && !GeneralHelper.IsItemOwned(parent)) return false;
            return !item.IsTag("STORAGE") || ContainerHelper.IsNestingAllowed(item, parent);
        };
        if (!string.IsNullOrEmpty(definition.sprite))
        {
            inventory.onSlotAddItemEndFunc += (_, _, _) => SetAppearance(bay, definition.sprite, inventory.childItems.Count);
            inventory.onSlotRemoveItemEndFunc += (_, _, _) => SetAppearance(bay, definition.sprite, inventory.childItems.Count);
        }
        ModHelper.SetSource(bay, Ids.ModId, Loc.Get("mod.name"));
        return bay;
    }

    private static void SetAppearance(GameItem bay, string spritePrefix, int itemCount)
    {
        int state = Math.Max(0, Math.Min(itemCount, 4));
        string sprite = state == 0 ? spritePrefix : spritePrefix + "_" + state;
        bay.SetSpriteAndShapeFromMod(Ids.ModId, sprite);
        SetFootprint(bay);
    }

    private static void SetFootprint(GameItem bay)
    {
        bay.SetShape(new GridShapeBuilder(4, 4).SetData(new byte[]
        {
            1, 1, 1, 1,
            1, 1, 1, 1,
            1, 1, 1, 1,
            1, 1, 1, 1
        }, 4));
    }
}
