using System;
using System.Collections.Generic;
using System.Linq;

namespace ElementalIndustry;

/// <summary>
/// Oxyhydrogen welding bench. It burns hydrogen and oxygen and replaces a
/// broken machine with a fresh working instance. Replacing is used because the target
/// version exposes no public "repair in place" call; all content and ids stay intact.
/// </summary>
internal static class WelderMachine
{
    public static GameItem Create(MachineDefinition definition)
    {
        var batterySlot = new GameSlotInventory();
        var moduleGrid = new GameGridInventory(4, 4);
        var hydrogenSlot = new GameSlotInventory();
        var oxygenSlot = new GameSlotInventory();
        var targetGrid = new GameGridInventory(5, 4);
        var outputGrid = new GameGridInventory(5, 4);

        PixelWindow window = MachineHelper.CreateMachineryWindow(
            true,
            MachineHelper.PowerPanel(batterySlot),
            MachineHelper.Panel(Loc.Get("panel.modules"), moduleGrid, "module"),
            MachineHelper.Panel(Loc.Get("panel.hydrogen"), hydrogenSlot, "gas_hydrogen"),
            MachineHelper.Panel(Loc.Get("panel.oxygen"), oxygenSlot, "gas_oxygen"),
            MachineHelper.Panel(Loc.Get("panel.broken_machine"), targetGrid, "input"),
            MachineHelper.Panel(Loc.Get("panel.repaired_machine"), outputGrid, "output"),
            MachineHelper.NotePanel(MachineFactory.NoteSlot(definition)));

        GameItem machine = MachineFactory.NewMachineItem(definition, window);
        MachineryHelper.InitMachinery(machine, definition.power);
        MachineHelper.SetupBatterySlot(batterySlot, machine);
        MachineHelper.SetupModuleBay(moduleGrid, machine);

        hydrogenSlot.mayInventoryAddItemFunc = (item, _) => GasContainer.Is(item, "H2");
        oxygenSlot.mayInventoryAddItemFunc = (item, _) => GasContainer.Is(item, "O2");
        targetGrid.mayInventoryAddItemFunc = (item, _) => item != null && item.unitCount == 1 && item.IsTag("BROKEN_MACHINE_TAG");
        outputGrid.mayInventoryAddItemFunc = (item, _) => item != null;

        machine.onCycleEndSlotItemFunc = (_, _, _) =>
        {
            int cycles = MachineFactory.WorkCycles(machine);
            for (int i = 0; i < cycles; i++)
                if (!Run(machine, batterySlot, hydrogenSlot, oxygenSlot, targetGrid, outputGrid, moduleGrid)) break;
        };
        return machine;
    }

    private static bool Run(
        GameItem machine,
        GameSlotInventory batterySlot,
        GameSlotInventory hydrogenSlot,
        GameSlotInventory oxygenSlot,
        GameGridInventory targetGrid,
        GameGridInventory outputGrid,
        GameGridInventory moduleGrid)
    {
        GameItem? broken = MachineFactory.Items(targetGrid).FirstOrDefault(item => item.unitCount == 1 && item.IsTag("BROKEN_MACHINE_TAG"));
        GameItem? hydrogen = hydrogenSlot.childItem;
        GameItem? oxygen = oxygenSlot.childItem;
        // Better weld quality wastes less torch gas. The 1:8 mass ratio is kept.
        int qualityFactor = 100 + Math.Clamp(MachineryHelper.GetCurrentQualityBonus(machine), -99, 900);
        int hydrogenGrams = Math.Max(1, (1000 + qualityFactor - 1) / qualityFactor);
        int oxygenGrams = hydrogenGrams * 8;
        if (broken == null || !GasContainer.Is(hydrogen, "H2") || !GasContainer.Is(oxygen, "O2") ||
            hydrogen!.unitCount < hydrogenGrams || oxygen!.unitCount < oxygenGrams)
        {
            return false;
        }

        int power = MachineryHelper.GetMachinePowerUsage(machine);
        GameItem? battery = batterySlot.childItem;
        if (power > 0 && (battery == null || battery.unitCount != 1 || !PowerHelper.CanDrawPowerSource(battery, power)))
        {
            return false;
        }
        GameItem repaired = DirectoryMaster.Item(broken.identifier);
        if (repaired == null)
        {
            return false;
        }
        var outputs = new List<GameItem> { repaired };
        if (!MachineFactory.TryPlaceOutputs(outputGrid, outputs))
        {
            return false;
        }
        if (power > 0 && (battery == null || battery.unitCount != 1 || !PowerHelper.DrawPowerSource(battery, power)))
        {
            MachineFactory.DestroyAll(outputs);
            return false;
        }

        GasContainer.Consume(hydrogen!, hydrogenGrams);
        GasContainer.Consume(oxygen!, oxygenGrams);
        broken.Destroy();
        MachineHelper.OnMachineActioned(machine, moduleGrid);
        return true;
    }
}
