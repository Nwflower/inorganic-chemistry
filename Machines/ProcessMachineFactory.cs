using System.Collections.Generic;
using System.Linq;

namespace ElementalIndustry;

/// <summary>
/// One data driven nightly processor. The same class backs the electrolyzer, furnaces,
/// reactors, presses and electronics machines; only their recipes and panel layout differ.
/// </summary>
internal static class ProcessMachineFactory
{
    public static GameItem Create(MachineDefinition definition)
    {
        var batterySlot = new GameSlotInventory();
        var waterSlot = new GameSlotInventory();
        var inputGrid = new GameGridInventory(definition.inputWidth, definition.inputHeight);
        var outputGrid = new GameGridInventory(definition.outputWidth, definition.outputHeight);
        var moduleGrid = new GameGridInventory(4, 4);
        var controls = new ProcessControlPanel(definition);

        var panels = new List<MachineryPanel>();
        if (definition.batterySlot)
        {
            panels.Add(MachineHelper.PowerPanel(batterySlot));
        }
        if (definition.moduleBay)
        {
            // Vanilla machine helpers expect the module bay immediately after power.
            panels.Add(MachineHelper.Panel(Loc.Get("panel.modules"), moduleGrid, "module"));
        }
        if (definition.waterSlot)
        {
            panels.Add(MachineHelper.Panel(Loc.Get("panel.water"), waterSlot, "water"));
        }
        // Machines whose recipes take no items (water electrolysis, air separation) get
        // no input panel: a grid that refuses everything is worse than no grid.
        if (ContentDatabase.RecipesFor(definition.id).Any(recipe => recipe.inputs.Length > 0))
        {
            panels.Add(MachineHelper.Panel(Loc.Get("panel.input"), inputGrid, "input"));
        }
        if (definition.outputSlot)
        {
            panels.Add(MachineHelper.Panel(Loc.Get("panel.output"), outputGrid, "output"));
        }
        panels.Add(MachineHelper.Panel(Loc.Get("panel.control"), controls.Root, "control"));
        panels.Add(MachineHelper.NotePanel(MachineFactory.NoteSlot(definition)));

        PixelWindow window = MachineHelper.CreateMachineryWindow(true, panels.ToArray());
        GameItem machine = MachineFactory.NewMachineItem(definition, window);
        controls.Bind(machine);
        window.openCallback = controls.Sync;
        machine.onOneSecondItemFunc = (_, _, _) => controls.UpdateStatus();
        MachineryHelper.InitMachinery(machine, definition.power);

        if (definition.batterySlot)
        {
            MachineHelper.SetupBatterySlot(batterySlot, machine);
        }
        if (definition.moduleBay)
        {
            MachineHelper.SetupModuleBay(moduleGrid, machine);
        }

        if (definition.waterSlot)
        {
            waterSlot.mayInventoryAddItemFunc = (item, _) => item != null && GeneralHelper.IsItemOwned(item) && item.IsTag("LIQUID_CONTAINER_TAG");
        }
        inputGrid.mayInventoryAddItemFunc = (item, _) => item != null && Processor.CanAcceptInput(definition, item);
        outputGrid.mayInventoryAddItemFunc = (item, _) => item != null && Processor.CanAcceptOutput(definition, item);

        machine.onCycleEndSlotItemFunc = (_, _, _) =>
            Processor.Run(machine, definition, batterySlot, waterSlot, inputGrid, outputGrid, moduleGrid);

        return machine;
    }
}
