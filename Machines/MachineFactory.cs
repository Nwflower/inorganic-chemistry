using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ElementalIndustry;

internal static class MachineFactory
{
    private const string PerformanceRemainderTag = "ELEM_PERFORMANCE_REMAINDER_INT";

    // Whole recipe batches preserve stoichiometric ratios; fractional performance
    // carries into the next night, as processing speed rather than free material.
    private static int WorkProgress(GameItem machine)
    {
        int performance = Math.Clamp(MachineryHelper.GetCurrentPerformanceBonus(machine), -100, 6300);
        return Math.Clamp(machine.GetTagReadonly(PerformanceRemainderTag).GetInt(), 0, 99) + 100 + performance;
    }

    internal static int PendingWorkCycles(GameItem machine) => WorkProgress(machine) / 100;

    internal static int WorkCycles(GameItem machine)
    {
        int progress = WorkProgress(machine);
        machine.ModifyTag(PerformanceRemainderTag, state => state.Enable().SetInt(progress % 100));
        return progress / 100;
    }
    public static GameItem Create(MachineDefinition definition)
    {
        return definition.archetype switch
        {
            "fuel_cell" => FuelCellMachine.Create(definition),
            "welder" => WelderMachine.Create(definition),
            _ => ProcessMachineFactory.Create(definition)
        };
    }

    internal static GameItem NewMachineItem(MachineDefinition definition, PixelWindow window)
    {
        GameItem machine = ItemDirectory.CreateEmptyItem(definition.id)
            .SetName(Loc.Get("machine." + definition.id + ".name"))
            .SetContentWindow(window);

        machine.shortDescription = Loc.Get("machine." + definition.id + ".desc");
        machine.unitValue = definition.value;
        machine.EnableTag("CONTAINER_TAG", skipValidate: true);
        machine.EnableTag("STANDARD_MACHINE_TAG", skipValidate: true);
        machine.EnableTag("manufacture", skipValidate: true);
        machine.SetGameItemType("MACHINE");
        if (definition.efficiencyRatio > 1)
        {
            machine.ModifyTag("MACHINE_EFFICIENCY_RATIO_INT", state => state.Enable().SetInt(definition.efficiencyRatio));
        }
        AudioHelper.InitMetalMachine(machine);

        if (!string.IsNullOrEmpty(definition.sprite))
        {
            machine.SetSpriteAndShapeFromMod(Ids.ModId, definition.sprite);
        }
        else if (!string.IsNullOrEmpty(definition.copySpriteFrom))
        {
            GameItem? template = DirectoryMaster.Item(definition.copySpriteFrom);
            if (template != null)
            {
                machine.SetSpriteAndShape(template.spriteAtlasPath, template.spritePath);
                template.Destroy();
            }
            else
            {
                machine.SetSpriteAndShapeFromMod(Ids.ModId, Ids.ElectrolysisCell);
            }
        }

        ModHelper.SetSource(machine, Ids.ModId, Loc.Get("mod.name"));
        return machine;
    }

    internal static GameSlotInventory NoteSlot(MachineDefinition definition)
    {
        var slot = new GameSlotInventory { identifier = "elem_note_" + definition.id };
        slot.mayInventoryAddItemFunc = (item, _) => item != null && item.identifier == MachineManual.NoteId(definition.id);
        slot.SetBackgroundFadeSprite("Items/items_tool", "postit");
        return slot;
    }

    internal static List<GameItem> Items(GameGridInventory grid)
    {
        return GraphUtils.FindAllChildrenType<GameItem>(grid);
    }

    internal static bool CanAccept(GameGridInventory grid, GameItem item)
    {
        return GraphUtils.CanAccept(grid, item);
    }

    /// <summary>
    /// Places a whole batch atomically. Gas may merge into an existing stack;
    /// its previous mass is restored if a later placement fails.
    /// </summary>
    internal static bool TryPlaceOutputs(GameGridInventory grid, IReadOnlyList<GameItem> outputs)
        => TryPlaceOutputs(grid, outputs, out _);

    internal static bool TryPlaceOutputs(GameGridInventory grid, IReadOnlyList<GameItem> outputs, out Action rollback)
    {
        rollback = () => { };
        // Do not destroy any object that was already in an inventory, even if an
        // external factory mistakenly returned it as an output.
        var owned = new List<GameItem>();
        var unique = new HashSet<GameItem>();
        foreach (GameItem? output in outputs)
        {
            if (output != null && output.parentInventory == null && unique.Add(output))
            {
                owned.Add(output);
            }
        }
        // Always transfer a whole stack: a partial transfer creates a split item
        // that cannot be safely rolled back after a later placement fails.
        if (owned.Count != outputs.Count || owned.Any(output => output.unitCount < 1))
        {
            DestroyAll(owned);
            return false;
        }

        var merged = new List<(GameItem target, int oldCount, int oldQuality)>();
        try
        {
            var placements = new List<GridShape>();
            var targets = new List<GameItem?>();
            foreach (GameItem output in owned)
            {
                GameItem? target = GasContainer.IsGasContainer(output)
                    ? Items(grid).FirstOrDefault(existing => GasContainer.IsGasContainer(existing) &&
                        !targets.Contains(existing) && CanMerge(grid, output, existing)) : null;
                GridShape? placement = target != null ? target.modifiedShape : FindOutputPlacement(grid, output, placements);
                if (placement == null)
                {
                    DestroyAll(owned);
                    return false;
                }
                placements.Add(placement);
                targets.Add(target);
            }

            for (int i = 0; i < owned.Count; i++)
            {
                GameItem output = owned[i];
                // Recheck against earlier placements and any synchronous callbacks.
                int amount = output.unitCount;
                GameItem? target = targets[i];
                SlotMarker? slot = target != null ? target.GetStackSlot(output) : grid.TryInventorySlot(output, amount, placements[i]);
                if (slot == null || slot.numTransfer < amount || slot.targetItem != target)
                {
                    RestoreMerged(merged);
                    DestroyAll(owned);
                    return false;
                }
                int oldCount = target?.unitCount ?? 0;
                int oldQuality = target != null ? ProductQuality.Get(target) : 0;
                int addedQuality = ProductQuality.Get(output);
                if (target != null) merged.Add((target, oldCount, oldQuality));
                if (slot.TryAcceptOnce(amount) != amount || (target == null && output.parentInventory != grid))
                {
                    RestoreMerged(merged);
                    DestroyAll(owned);
                    return false;
                }
                if (target != null)
                    ProductQuality.Set(target, ProductQuality.Weighted(oldQuality, oldCount, addedQuality, amount));
            }
            for (int i = 0; i < owned.Count; i++)
                if (targets[i] != null) owned[i].Destroy();
            rollback = () => { RestoreMerged(merged); DestroyAll(owned); };
            return true;
        }
        catch (Exception exception)
        {
            RestoreMerged(merged);
            DestroyAll(owned);
            Debug.LogError($"[{Ids.ModId}] cannot place machine outputs: {exception}");
            return false;
        }
    }

    private static bool CanMerge(GameGridInventory grid, GameItem output, GameItem existing)
    {
        SlotMarker? slot = existing.GetStackSlot(output);
        return slot != null && slot.targetItem == existing && slot.numTransfer >= output.unitCount;
    }

    private static void RestoreMerged(List<(GameItem target, int oldCount, int oldQuality)> merged)
    {
        for (int i = merged.Count - 1; i >= 0; i--)
        {
            merged[i].target.SetUnitCount(merged[i].oldCount);
            ProductQuality.Set(merged[i].target, merged[i].oldQuality);
        }
    }

    private static GridShape? FindOutputPlacement(GameGridInventory grid, GameItem item, List<GridShape> reserved)
    {
        // Match the vanilla grid's non-stacking position search order and use its
        // own slot validation; reserved shapes model earlier outputs in the batch.
        var shape = new GridShapeBuilder(item.modifiedShape);
        for (int flip = 0; flip < 2; flip++)
        {
            for (int rotation = 0; rotation < 4; rotation++)
            {
                for (int y = 0; y < grid.inventoryShape.height; y++)
                {
                    for (int x = 0; x < grid.inventoryShape.width; x++)
                    {
                        GridShape candidate = shape.SetTransform(x, y, flip != 0, 4 - rotation).Build();
                        if (reserved.Any(existing => candidate.Intersects(existing)))
                        {
                            continue;
                        }
                        SlotMarker slot = grid.TryInventorySlot(item, item.unitCount, candidate);
                        if (slot != null && slot.numTransfer >= item.unitCount && slot.targetItem == null)
                        {
                            return candidate;
                        }
                    }
                }
            }
        }
        return null;
    }

    internal static void DestroyAll(IEnumerable<GameItem?> items)
    {
        foreach (GameItem? item in items)
        {
            if (item != null)
            {
                item.Destroy();
            }
        }
    }
}
