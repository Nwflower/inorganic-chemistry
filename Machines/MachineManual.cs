using System;
using System.Linq;
using HarmonyLib;

namespace ElementalIndustry;

/// <summary>Vanilla-style removable instruction notes. Factory creation remains empty so save loading does not duplicate notes.</summary>
internal static class MachineManual
{
    internal static string NoteId(string machineId) => "elem_manual_" + machineId;

    internal static GameItem Create(MachineDefinition definition)
    {
        GameItem note = ItemDirectory.CreateEmptyItem(NoteId(definition.id));
        GameItem template = DirectoryMaster.Item("postit");
        note.SetSpriteAndShape(template.spriteAtlasPath, template.spritePath);
        template.Destroy();
        note.SetName(Loc.Get("manual.note.title", Loc.Get("machine." + definition.id + ".name")));
        note.shortDescription = Loc.Get("machine." + definition.id + ".manual_short");
        note.flavorText = Loc.Get("machine." + definition.id + ".manual");
        note.SetGameItemType("DOCUMENT");
        note.EnableTag("IMPORTANT_TAG", skipValidate: true);
        note.EnableTag("MACHINE_MANUAL_TAG", skipValidate: true);
        note.unitValue = 0;
        return note;
    }

    internal static void AddToNewMachine(GameItem machine)
    {
        if (!ContentDatabase.Machines.TryGetValue(machine.identifier, out MachineDefinition? definition) ||
            definition.archetype == "fuel_cell") return;
        GameSlotInventory? slot = GraphUtils.FindAllChildrenType<GameSlotInventory>(machine)
            .FirstOrDefault(s => s.identifier == "elem_note_" + definition.id);
        if (slot == null || slot.childItem != null) return;
        // Construct through the directory so the game assigns ownership and a unique id.
        GameItem note = DirectoryMaster.Item(NoteId(definition.id));
        // TryAcceptAll returns the number left over, not the number inserted.
        if (GraphUtils.TryAcceptAll(slot, note) != 0) note.Destroy();
    }

    // The game clears factory-created children inside DirectoryMaster.Item. Attach
    // notes afterwards, and never create extra children while restoring a save.
    [HarmonyPatch(typeof(DirectoryMaster), nameof(DirectoryMaster.Item), new[] { typeof(string), typeof(bool) })]
    internal static class NewMachineManualPatch
    {
        private static void Postfix(GameItem __result)
        {
            if (__result != null && !SaveManager.IsDecoding) AddToNewMachine(__result);
        }
    }
}
