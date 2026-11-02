using System;
using HarmonyLib;

namespace ElementalIndustry;

/// <summary>A power source with fuel, oxidizer and water-container slots.</summary>
internal static class FuelCellMachine
{
    internal const int HydrogenGramsPerEnergy = 10;
    internal const int OxygenPerHydrogen = 8;
    internal const int WaterMicrolitresPerHydrogen = 9000;
    private const string ReserveTag = "ELEM_FUEL_CELL_RESERVE_INT";

    public static GameItem Create(MachineDefinition definition)
    {
        // Both slots accept 16x48 gas bottles, so their empty footprint matches them.
        var fuel = new GameSlotInventory(1, 3) { identifier = "elem_fuel_slot" };
        var oxidizer = new GameSlotInventory(1, 3) { identifier = "elem_oxidizer_slot" };
        var waterOutput = new GameSlotInventory { identifier = "elem_water_output_slot" };
        GameSlotInventory noteSlot = MachineFactory.NoteSlot(definition);
        fuel.mayInventoryAddItemFunc = (item, _) => GasContainer.Is(item, "H2") && GeneralHelper.IsItemOwned(item);
        oxidizer.mayInventoryAddItemFunc = (item, _) => GasContainer.Is(item, "O2") && GeneralHelper.IsItemOwned(item);
        waterOutput.mayInventoryAddItemFunc = (item, _) => item != null && item.unitCount == 1 && item.IsTag("LIQUID_CONTAINER_TAG") && GeneralHelper.IsItemOwned(item);
        waterOutput.SetBackgroundFadeSprite("Items/water_container4", "xl");
        PixelWindow window = MachineHelper.CreateMachineryWindow(true,
            MachineHelper.Panel(Loc.Get("panel.fuel"), fuel, "fuel"),
            MachineHelper.Panel(Loc.Get("panel.oxygen_optional"), oxidizer, "oxidizer"),
            MachineHelper.Panel(Loc.Get("panel.water_output"), waterOutput, "water"),
            MachineHelper.NotePanel(noteSlot));
        GameItem cell = ItemDirectory.CreateEmptyItem(definition.id).SetContentWindow(window)
            .SetName(Loc.Get("machine." + definition.id + ".name"));
        cell.shortDescription = Loc.Get("machine." + definition.id + ".desc");
        cell.SetSpriteAndShapeFromMod(Ids.ModId, definition.sprite);
        SetFootprint(cell);
        fuel.onSlotAddItemEndFunc += (_, _, _) => UpdateAppearance(cell);
        fuel.onSlotRemoveItemEndFunc += (_, _, _) => UpdateAppearance(cell);
        oxidizer.onSlotAddItemEndFunc += (_, _, _) => UpdateAppearance(cell);
        oxidizer.onSlotRemoveItemEndFunc += (_, _, _) => UpdateAppearance(cell);
        cell.onOneSecondItemFunc = (_, _, _) => UpdateAppearance(cell);
        MachineFactory.ApplyBaseValue(cell, definition);
        cell.EnableTag("CONTAINER_TAG", skipValidate: true);
        PowerHelper.InitPowerSourceItem(cell, 0, 0);
        ContrabandHelper.InitContrabandItem(cell, 1);
        cell.ModifyTag(ReserveTag, s => s.Enable().SetInt(0));
        // The vanilla converter also excludes rechargers and equipment. Ship power
        // racks use a separate accounting path and are excluded here as well.
        cell.mayInventoryAddItemFunc = (_, inv) =>
        {
            GameItem? parent = inv?.GetParentItem();
            return parent == null || (!parent.IsID("recharger") && !parent.IsTag("power_source_furniture") &&
                   !parent.IsTag(EquipmentHelper.EQUIPMENT_TAG) && !parent.IsTag("COMPLEXE_EQUIPMENT_TAG"));
        };
        ModHelper.SetSource(cell, Ids.ModId, Loc.Get("mod.name"));
        return cell;
    }

    internal static bool IsCell(GameItem? item) => item != null && item.identifier == Ids.FuelCell;
    private static GameSlotInventory? Slot(GameItem cell, int index) => MachineHelper.GetBodyStorage(cell, index) as GameSlotInventory;

    private static void SetFootprint(GameItem cell) =>
        cell.SetShape(new GridShapeBuilder(4, 2).SetData(new byte[]
        {
            1, 1, 1, 1,
            1, 1, 1, 1
        }, 4));

    private static void UpdateAppearance(GameItem cell)
    {
        bool hydrogen = GasContainer.Is(Slot(cell, 0)?.childItem, "H2");
        bool oxygen = hydrogen && GasContainer.Is(Slot(cell, 1)?.childItem, "O2");
        string sprite = Ids.FuelCell + (oxygen ? "_h2_o2" : hydrogen ? "_h2" : "");
        if (cell.spritePath == sprite) return;
        cell.SetSpriteAndShapeFromMod(Ids.ModId, sprite);
        SetFootprint(cell);
    }

    internal static int Reserve(GameItem cell) => Math.Max(0, cell.GetTagReadonly(ReserveTag).GetInt());

    internal static long AvailableEnergy(GameItem cell)
    {
        GameItem? fuel = Slot(cell, 0)?.childItem;
        int fuels = GasContainer.Is(fuel, "H2") ? Math.Max(0, fuel!.unitCount) : 0;
        return (long)Reserve(cell) + fuels / HydrogenGramsPerEnergy;
    }

    internal static bool CanSupply(GameItem cell, int amount) => amount >= 0 && AvailableEnergy(cell) >= amount;

    internal static bool Supply(GameItem cell, int amount)
    {
        if (!CanSupply(cell, amount)) return false;
        int reserve = Reserve(cell);
        int fromReserve = Math.Min(reserve, amount);
        reserve -= fromReserve;
        int remaining = amount - fromReserve;
        if (remaining > 0)
        {
            // CanSupply bounds this product by the available (32-bit) hydrogen mass.
            int fuelGrams = remaining * HydrogenGramsPerEnergy;
            GameItem fuel = Slot(cell, 0)!.childItem!;
            GameItem? oxygen = Slot(cell, 1)?.childItem;
            GameItem? water = Slot(cell, 2)?.childItem;
            int reacted = 0;
            if (GasContainer.Is(oxygen, "O2") && water != null && water.unitCount == 1 && water.IsTag("LIQUID_CONTAINER_TAG"))
            {
                int oxygenAvailable = oxygen!.unitCount / OxygenPerHydrogen;
                int containerCapacity = Math.Max(0, WaterHelper.GetFreeCapacity(water)) / WaterMicrolitresPerHydrogen;
                reacted = Math.Min(fuelGrams, Math.Min(oxygenAvailable, containerCapacity));
            }
            if (reacted > 0)
            {
                bool wasEmpty = WaterHelper.IsEmpty(water!);
                WaterHelper.AddLiquid(water!, "water", reacted * WaterMicrolitresPerHydrogen);
                WaterHelper.UpdateContainerGoodType(water!);
                if (!water!.IsTag("WINE_CONTAINER_TAG"))
                {
                    if (wasEmpty)
                        WaterFeatureHelper.SetPresumedPurity(water, 10000);
                    else
                        water.unitValue = WaterFeatureHelper.GetWaterPrice(WaterHelper.GetTotalVolume(water));
                }
                GasContainer.Consume(oxygen!, reacted * OxygenPerHydrogen);
            }
            GasContainer.Consume(fuel, fuelGrams);
        }
        cell.ModifyTag(ReserveTag, s => s.Enable().SetInt(reserve));
        UpdateAppearance(cell);
        return true;
    }

    [HarmonyPatch(typeof(PowerHelper), nameof(PowerHelper.CanDrawPowerSource))]
    internal static class CanDrawPatch
    {
        private static bool Prefix(GameItem gameItem, int amount, ref bool __result)
        {
            if (!IsCell(gameItem)) return true;
            __result = CanSupply(gameItem, amount);
            return false;
        }
    }

    [HarmonyPatch(typeof(PowerHelper), nameof(PowerHelper.DrawPowerSource))]
    internal static class DrawPatch
    {
        private static bool Prefix(GameItem gameItem, int amount, ref bool __result)
        {
            if (!IsCell(gameItem)) return true;
            __result = Supply(gameItem, amount);
            return false;
        }
    }
}
