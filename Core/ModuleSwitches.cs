using System;
using System.Collections.Generic;
using UnityEngine;

namespace ElementalIndustry;

/// <summary>
/// Two level switches: process modules and individual buildings. Both are normal
/// ModConfig entries, so a disabled module is absent from item/machine registration
/// on the next start instead of leaving broken half-registered content.
/// </summary>
internal static class ModuleSwitches
{
    private static readonly Dictionary<string, ConfigEntry<bool>> ModuleEntries = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, ConfigEntry<bool>> BuildingEntries = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, bool> ModuleDefaults = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, bool> BuildingDefaults = new(StringComparer.Ordinal);

    public static void Initialize()
    {
        ModuleEntries.Clear();
        BuildingEntries.Clear();
        ModuleDefaults.Clear();
        BuildingDefaults.Clear();

        ModConfig config = ModHelper.GetConfig(Ids.ModId);
        foreach (ModuleDefinition module in ContentDatabase.Modules.Values)
        {
            ModuleDefaults[module.id] = module.defaultEnabled;
            string key = "module." + module.id;
            ModuleEntries[module.id] = config.Define(
                key,
                module.defaultEnabled,
                "模块 / Modules",
                () => Loc.Get(module.nameKey),
                () => Loc.Get("config.module.tooltip"),
                requiresRestart: true);
        }

        foreach (BuildingDefinition building in ContentDatabase.Buildings.Values)
        {
            BuildingDefaults[building.id] = building.defaultEnabled;
            string key = "building." + building.id;
            BuildingEntries[building.id] = config.Define(
                key,
                building.defaultEnabled,
                "建筑 / Buildings",
                () => Loc.Get("machine." + building.id + ".name"),
                () => Loc.Get("config.building.tooltip"),
                requiresRestart: true);
        }
    }

    public static bool IsModuleEnabled(string moduleId)
    {
        return IsModuleEnabled(moduleId, new HashSet<string>(StringComparer.Ordinal));
    }

    private static bool IsModuleEnabled(string moduleId, HashSet<string> visiting)
    {
        if (!visiting.Add(moduleId))
        {
            Debug.LogError($"[{Ids.ModId}] module dependency cycle at {moduleId}");
            return false;
        }

        bool enabled = ModuleEntries.TryGetValue(moduleId, out ConfigEntry<bool>? entry)
            ? entry.Value
            : ModuleDefaults.TryGetValue(moduleId, out bool value) && value;
        if (!enabled)
        {
            visiting.Remove(moduleId);
            return false;
        }

        if (ContentDatabase.Modules.TryGetValue(moduleId, out ModuleDefinition? definition))
        {
            foreach (string dependency in definition.dependsOn)
            {
                if (!IsModuleEnabled(dependency, visiting))
                {
                    visiting.Remove(moduleId);
                    return false;
                }
            }
        }
        visiting.Remove(moduleId);
        return true;
    }

    public static bool IsBuildingEnabled(string buildingId)
    {
        if (BuildingEntries.TryGetValue(buildingId, out ConfigEntry<bool>? entry))
        {
            return entry.Value;
        }
        return BuildingDefaults.TryGetValue(buildingId, out bool value) ? value : true;
    }

    public static bool IsContentEnabled(string moduleId, string buildingId = "")
    {
        if (!IsModuleEnabled(moduleId))
        {
            return false;
        }
        return string.IsNullOrEmpty(buildingId) || IsBuildingEnabled(buildingId);
    }

    public static void LogState(ModLog? log)
    {
        foreach (ModuleDefinition module in ContentDatabase.Modules.Values)
        {
            log?.Log($"Module {module.id}: {(IsModuleEnabled(module.id) ? "on" : "off")}");
        }
    }
}
