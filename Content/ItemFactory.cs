using System;
using UnityEngine;

namespace ElementalIndustry;

internal static class ItemFactory
{
    public static GameItem Create(string id)
    {
        if (!ContentDatabase.Items.TryGetValue(id, out ItemDefinition? definition))
        {
            GameItem fallback = DirectoryMaster.Item(id);
            if (fallback == null)
            {
                throw new InvalidOperationException($"Unknown item id: {id}");
            }
            return fallback;
        }
        return Create(definition);
    }

    public static GameItem CreateAny(string id)
    {
        return ContentDatabase.Items.ContainsKey(id) ? Create(id) : CreateVanilla(id);
    }

    public static GameItem Create(ItemDefinition definition)
    {
        if (definition.id == MedicineCabinet.ItemId)
        {
            return MedicineCabinet.Create(definition);
        }
        if (definition.id == LaboratoryBay.ItemId)
        {
            return LaboratoryBay.Create(definition);
        }

        GameItem item = ItemDirectory.CreateEmptyItem(definition.id);
        item.SetName(Loc.Get("item." + definition.id + ".name"));
        item.shortDescription = Loc.Get("item." + definition.id + ".desc");
        item.unitValue = definition.value;

        string[] types = definition.itemTypes.Length > 0 ? definition.itemTypes : new[] { "MATERIAL", "BASIC_MATERIAL" };
        foreach (string type in types)
        {
            if (!string.IsNullOrEmpty(type))
            {
                item.SetGameItemType(type);
            }
        }

        foreach (string tag in definition.tags)
        {
            if (!string.IsNullOrEmpty(tag))
            {
                item.EnableTag(tag, skipValidate: true);
            }
        }

        foreach (string hazard in definition.hazards)
        {
            if (!string.IsNullOrEmpty(hazard))
            {
                item.EnableTag("ELEM_HAZARD_" + hazard.ToUpperInvariant(), skipValidate: true);
            }
        }

        ApplySprite(item, definition);

        if (!string.IsNullOrEmpty(definition.gas))
        {
            GasContainer.Init(item, definition.gas);
            if (definition.stackable)
            {
                GasContainer.MakeStackable(item);
            }
        }

        if (!string.IsNullOrEmpty(definition.chemLiquid))
        {
            ChemicalHelper.InitChemItem(item, definition.blendable, definition.soluble);
            if (definition.chemML > 0)
            {
                ChemicalHelper.SetChemContentML(item, definition.chemLiquid, definition.chemML);
            }
        }

        if (!string.IsNullOrEmpty(definition.chemicalProductIngredient))
        {
            ChemicalHelper.InitChemItem(item, definition.blendable, definition.soluble);
            // Grade only after the vanilla extractor transfers the selected liquid.
            item.EnableTag(ChemicalProductHelper.CHEMICAL_PRODUCT_TAG);
            item.ModifyTag(ChemicalProductHelper.ACTIVATE_INGREDIENT_TAG,
                state => state.Enable().SetString(definition.chemicalProductIngredient));
        }

        if (definition.ingot)
        {
            IngotPurityHelper.InitIngotItem(item);
        }

        if (definition.id == "elem_coke")
        {
            // The vanilla bottle and aquaculture filters use these tags and durability.
            item.SetGameItemType("FILTER");
            item.SetGameItemType("WATER_PURIFICATION_SUPPLY");
            item.EnableTag("WATER_FILTER", skipValidate: true);
            item.ModifyTag("FILTER_SIZE_TAG", s => s.Enable().SetInt(22), skipValidate: true);
            DurabilityHelper.InitItemWithDurability(item, 200);
        }
        if (definition.id == FurnaceChemistry.ModuleId)
        {
            ModuleHelper.InitModuleItem(item, "MODULE_TYPE_FURNACE", additionalEffectDisplay: Loc.Get("item.elem_furnace_chem_module.desc"));
        }

        if (definition.id == Ids.OxygenCylinder)
        {
            GasUses.ConfigureOxygenCylinder(item);
        }

        if (MedicineCabinet.IsModStackableMedicine(definition))
        {
            item.mayStackItemFunc = MedicineCabinet.MayStackInCabinet;
        }

        ModHelper.SetSource(item, Ids.ModId, Loc.Get("mod.name"));
        return item;
    }

    private static void ApplySprite(GameItem item, ItemDefinition definition)
    {
        if (!string.IsNullOrEmpty(definition.sprite))
        {
            item.SetSpriteAndShapeFromMod(Ids.ModId, definition.sprite);
            return;
        }

        if (!string.IsNullOrEmpty(definition.copySpriteFrom))
        {
            GameItem? template = CreateVanilla(definition.copySpriteFrom);
            if (template != null)
            {
                item.SetSpriteAndShape(template.spriteAtlasPath, template.spritePath);
                template.Destroy();
                return;
            }
        }

        // Last-resort sprite: a small vanilla material icon. Content validation warns
        // about this through the missing sprite key in Data/items.json.
        GameItem? fallback = CreateVanilla("common_ore");
        if (fallback != null)
        {
            item.SetSpriteAndShape(fallback.spriteAtlasPath, fallback.spritePath);
            fallback.Destroy();
        }
    }

    private static GameItem CreateVanilla(string id)
    {
        return DirectoryMaster.Item(id);
    }
}
