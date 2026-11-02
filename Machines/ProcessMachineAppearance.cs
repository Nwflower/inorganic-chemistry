using System.Linq;

namespace ElementalIndustry;

/// <summary>Independent power and contents indicators, not a promise that a batch can run.</summary>
internal static class ProcessMachineAppearance
{
    internal static void Bind(GameItem machine, MachineDefinition definition,
        GameSlotInventory battery, GameSlotInventory water, GameGridInventory input,
        GameGridInventory modules, PixelWindow window)
    {
        if (definition.id != "elem_air_separator" && definition.id != "elem_rust_extractor" &&
            definition.id != "elem_chip_fab") return;
        if (string.IsNullOrEmpty(definition.sprite)) return;

        RecipeDefinition[] recipes = ContentDatabase.RecipesFor(definition.id).ToArray();
        void Refresh()
        {
            // Battery charge can change without insertion/removal. Use the same
            // module-adjusted draw and single-battery rule as the processor.
            bool powered = recipes.Any(recipe =>
                Processor.CanDraw(battery.childItem, Processor.RecipePower(machine, definition, recipe)));
            bool loaded = false;
            if (definition.id == "elem_rust_extractor" && water.childItem != null)
            {
                // Show actual feedstock, even below a full batch; clean water
                // must not show rust. No liquid is consumed by this check.
                loaded = recipes.Any(recipe => !string.IsNullOrEmpty(recipe.waterContaminant) &&
                    WaterHelper.GetContaminantAmount(water.childItem, recipe.waterContaminant) > 0);
            }
            else if (definition.id == "elem_chip_fab")
            {
                loaded = MachineFactory.Items(input).Any(item => Processor.CanAcceptInput(definition, item));
            }

            string sprite = definition.sprite + (powered ? "_powered" : "") + (loaded ? "_loaded" : "");
            // Consult the actual path, not a cached state: loading a save can
            // restore the sprite after construction and before onLoaded.
            if (machine.spritePath != sprite)
                machine.SetSpriteAndShapeFromMod(Ids.ModId, sprite);
            // The state sprite's sparse top-right cell derives as empty. Reapply the
            // full footprint even when the path is unchanged on a save load.
            MachineFactory.ApplyShape(machine, definition.id);
        }

        foreach (GameInventory inventory in new GameInventory[] { battery, water, input, modules })
        {
            inventory.onSlotAddItemEndFunc += (_, _, _) => Refresh();
            inventory.onSlotRemoveItemEndFunc += (_, _, _) => Refresh();
            inventory.onSlotChangeAmountItemFunc += (_, _, _) => Refresh();
        }
        machine.onLoaded += _ => Refresh();
        machine.onOneSecondItemFunc += (_, _, _) => Refresh();
        machine.onCycleEndSlotItemFunc += (_, _, _) => Refresh();
        window.openCallback += Refresh;
        Refresh();
    }
}
