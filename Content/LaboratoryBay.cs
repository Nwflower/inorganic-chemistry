namespace ElementalIndustry;

/// <summary>A larger work area built with the same inventory and nesting rules as the vanilla machine bay.</summary>
internal static class LaboratoryBay
{
    internal const string ItemId = "elem_laboratory_bay";

    internal static GameItem Create(ItemDefinition definition)
    {
        (PixelWindow window, GameInventory inventory) = DirectoryUtils.CreateInventoryWindow(14, 18);
        GameItem bay = ItemDirectory.CreateEmptyItem(definition.id).SetContentWindow(window);
        GameItem template = DirectoryMaster.Item("machine_bay_ext");
        bay.SetSpriteAndShape(template.spriteAtlasPath, template.spritePath);
        template.Destroy();
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
        ModHelper.SetSource(bay, Ids.ModId, Loc.Get("mod.name"));
        return bay;
    }
}
