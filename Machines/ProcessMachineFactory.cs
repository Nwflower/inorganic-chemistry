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
        var moduleGrid = new GameGridInventory(MachineHelper.DEFAULT_MODULE_BAY_CONFIG, MachineHelper.DEFAULT_MODULE_BAY_WIDTH);
        // Electrolysis and rust extraction choose their path from the liquid, so they
        // have no recipe control panel. The gas synthesizer uses the finisher-style
        // configure/run panel; the assembler prints on demand; air separation runs
        // without a redundant status panel.
        GameItem? machine = null;
        ProcessControlPanel? controls = definition.id == Ids.ElectrolysisCell ||
            definition.id == Ids.AirSeparator || definition.id == Ids.RustExtractor ||
            definition.id == Ids.ChemicalReactor
            ? null
            : new ProcessControlPanel(definition, definition.id == Ids.ChipFab
                ? () => Processor.Run(machine!, definition, batterySlot, waterSlot, inputGrid, outputGrid, moduleGrid)
                : null);
        GasSynthesisControlPanel? gasControls = null;
        if (definition.id == Ids.ChemicalReactor)
            gasControls = new GasSynthesisControlPanel(definition, () => Processor.Run(
                machine!, definition, batterySlot, waterSlot, inputGrid, outputGrid, moduleGrid));

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
        // Machines whose recipes take no items (electrolysis, rust extraction and air
        // separation) get no input panel: a grid that refuses everything is worse than none.
        if (ContentDatabase.RecipesFor(definition.id).Any(recipe => recipe.inputs.Length > 0))
        {
            panels.Add(MachineHelper.Panel(Loc.Get("panel.input"), inputGrid, "input"));
        }
        if (definition.outputSlot)
        {
            panels.Add(MachineHelper.Panel(Loc.Get("panel.output"), outputGrid, "output"));
        }
        if (controls != null)
            panels.Add(MachineHelper.Panel(Loc.Get("panel.control"), controls.Root, "control"));
        if (gasControls != null)
            panels.Add(MachineHelper.Panel(Loc.Get("panel.control"), gasControls.Root, "control"));
        panels.Add(MachineHelper.NotePanel(MachineFactory.NoteSlot(definition)));

        PixelWindow window = MachineHelper.CreateMachineryWindow(true, panels.ToArray());
        machine = MachineFactory.NewMachineItem(definition, window);
        controls?.Bind(machine);
        gasControls?.Bind(machine);
        window.openCallback = () => { controls?.Sync(); gasControls?.Sync(); };
        machine.onOneSecondItemFunc = (_, _, _) => { controls?.UpdateStatus(); gasControls?.Sync(); };
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
            waterSlot.SetBackgroundFadeSprite("Items/water_container4", "xl");
            waterSlot.slotPriorityFunc = (_, _, _) => 5;
        }
        inputGrid.mayInventoryAddItemFunc = (item, _) => item != null && Processor.CanAcceptInput(definition, item);
        outputGrid.mayInventoryAddItemFunc = (item, _) => item != null && Processor.CanAcceptOutput(definition, item);

        if (definition.id != Ids.ChemicalReactor && definition.id != Ids.ChipFab)
            machine.onCycleEndSlotItemFunc = (_, _, _) =>
                Processor.Run(machine, definition, batterySlot, waterSlot, inputGrid, outputGrid, moduleGrid);

        ProcessMachineAppearance.Bind(machine, definition, batterySlot, waterSlot, inputGrid, moduleGrid, window);
        return machine;
    }
}
