using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using HarmonyLib;

namespace ElementalIndustry;

/// <summary>
/// Official loader entry point. All catalog data is loaded in the main-menu lifecycle;
/// item factories are created later through ModHook.OnModItemDirectoryInit.
/// </summary>
public sealed class ModEntry : IMod
{
    private ModManifest? _manifest;
    private ModLog? _log;
    private bool _ready;
    private bool _hooked;
    private ModItemDirectory? _pendingDirectory;
    private bool _factoriesRegistered;
    private Harmony? _harmony;

    public void Init(ModManifest manifest)
    {
        _manifest = manifest;
        _log = new ModLog(manifest);
        _log.Log($"{manifest.Name} v{manifest.ModVersion} initialized.");
    }

    public void OnEnable()
    {
        if (_hooked)
        {
            return;
        }

        _harmony = new Harmony("elemental_industry.gameplay");
        ApplyPatches();

        ModHook.OnModItemDirectoryInit += OnModItemDirectoryInit;
        ModHook.OnCreateTooltipLate += OnCreateTooltipLate;
        ModHook.OnLootTablesLoaded += OnLootTablesLoaded;
        ModHook.OnNewGameStarted += OnNewGameStarted;

        // 模组名跟着语言走：语言就绪后再改 manifest.Name，不能在 OnEnable 里判定语言。
        if (_manifest != null) LocalizedModName.Schedule(_manifest);

        _hooked = true;

        // The loader registers this mod only after OnEnable, so this first attempt is
        // expected to miss and is retried from the hooks above.
        TryInitialize();
    }

    /// <summary>
    /// 装配程序集里所有带 <c>[HarmonyPatch]</c> 的补丁类。注意加载器在
    /// <c>ModLoader.Awake</c> 阶段已经对整个程序集 <c>PatchAll</c> 过一次，所以带特性的补丁
    /// 无论如何都会在加载期生效；会 JIT 到「静态构造里取本地化字符串」的类型的补丁必须
    /// 去掉特性、改走 <see cref="DeferredPatchRunner"/>（见
    /// docs/experience/harmony-patch-jit-triggers-localization-ctor.md）。
    /// </summary>
    private void ApplyPatches()
    {
        int applied = 0;
        foreach (Type type in typeof(ModEntry).Assembly.GetTypes())
        {
            if (type.GetCustomAttributes(typeof(HarmonyPatch), inherit: false).Length == 0) continue;
            if (ApplyPatch(type)) applied++;
        }
        _log?.Log($"Harmony: {applied} patch class(es) applied.");
    }

    private bool ApplyPatch(Type type)
    {
        try
        {
            _harmony?.CreateClassProcessor(type).Patch();
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError($"[{Ids.ModId}] cannot patch {type.FullName}: {exception.Message}");
            return false;
        }
    }

    public void OnDisable()
    {
        DeferredPatchRunner.Cancel();
        LocalizedModName.Reset();
        ModHook.OnModItemDirectoryInit -= OnModItemDirectoryInit;
        ModHook.OnCreateTooltipLate -= OnCreateTooltipLate;
        ModHook.OnLootTablesLoaded -= OnLootTablesLoaded;
        ModHook.OnNewGameStarted -= OnNewGameStarted;
        _hooked = false;

        ModHelper.RemoveAfterhourLocations(Ids.ModId);
        ModHelper.RemoveDebugFunctions(Ids.ModId);
        ModHelper.RemoveStartingScenarios(Ids.ModId);
        ModHelper.RemovePaperBooks(Ids.ModId);
        NativeChemicalProducts.Unregister();
        InorganicLiquids.Unregister();
        _harmony?.UnpatchAll("elemental_industry.gameplay");
        _harmony = null;
        _ready = false;
        _factoriesRegistered = false;
        _pendingDirectory = null;
        _log?.Log("Disabled.");
    }

    private void OnNewGameStarted(string saveId)
    {
        TryInitialize();
    }

    /// <summary>
    /// Loads content once the loader has registered this mod, then performs the one-time
    /// registrations. Safe to call from every hook; it is a no-op afterwards.
    /// </summary>
    private bool TryInitialize()
    {
        if (_ready)
        {
            return true;
        }
        if (!ContentDatabase.Load())
        {
            if (!ContentDatabase.NotReady)
            {
                _log?.Log("Content load failed; registration skipped.");
            }
            return false;
        }

        ModuleSwitches.Initialize();
        ModuleSwitches.LogState(_log);
        ModHelper.AddAfterhourLocation(new ChemistShopLocation());
        RegisterDebugFunctions();
        RegisterStartingScenario();
        _ready = true;
        if (_pendingDirectory != null)
        {
            RegisterFactories(_pendingDirectory);
        }
        _log?.Log("Enabled: Inorganic Chemistry Basics content registered.");
        return true;
    }

    private void OnModItemDirectoryInit(ModItemDirectory directory)
    {
        // Keep the directory even if content is not loaded yet: it is created once, so
        // factories must be registered into this same instance once loading succeeds.
        _pendingDirectory = directory;
        TryInitialize();
    }

    private void RegisterFactories(ModItemDirectory directory)
    {
        if (_factoriesRegistered)
        {
            return;
        }
        if (_harmony != null) InorganicLiquids.Register(_harmony);
        _factoriesRegistered = true;

        int items = 0;
        foreach (ItemDefinition definition in ContentDatabase.Items.Values)
        {
            if (!ContentDatabase.IsModuleEnabled(definition.module))
            {
                continue;
            }
            ItemDefinition captured = definition;
            directory.Add(captured.id, () => ItemFactory.Create(captured));
            items++;
        }

        int machines = 0;
        foreach (MachineDefinition definition in ContentDatabase.Machines.Values)
        {
            if (!ContentDatabase.IsContentEnabled(definition.module, definition.id))
            {
                continue;
            }
            MachineDefinition captured = definition;
            directory.Add(captured.id, () => MachineFactory.Create(captured));
            directory.Add(MachineManual.NoteId(captured.id), () => MachineManual.Create(captured));
            machines++;
        }

        if (_harmony != null) NativeChemicalProducts.Register(_harmony);

        _log?.Log($"Registered {items} item and {machines} machine factories.");
    }

    private void OnLootTablesLoaded()
    {
        if (!TryInitialize() || !ContentDatabase.IsModuleEnabled("WorldIntegration"))
        {
            return;
        }
        foreach (LootPatchDefinition patch in ContentDatabase.LootPatches)
        {
            foreach (ItemAmountDefinition entry in patch.add)
            {
                if (string.IsNullOrEmpty(entry.id))
                {
                    continue;
                }
                if (ContentDatabase.Items.TryGetValue(entry.id, out ItemDefinition? definition) &&
                    !ContentDatabase.IsModuleEnabled(definition.module))
                {
                    continue;
                }
                ModHelper.AddLootEntry(Ids.ModId, patch.table, entry.id, patch.weight);
            }
        }
    }

    private void OnCreateTooltipLate(RichTextBuilder builder, GameItem item)
    {
        if (item == null || !TryInitialize())
        {
            return;
        }

        if (item.IsTag(GasContainer.GasContainerTag))
        {
            string gasType = GasContainer.GetGasType(item);
            builder.AddLine($"{Loc.Get("tooltip.gas")}: {GasContainer.GetDisplayGas(gasType)}", ignoreEmpty: true);
            builder.AddLine(Loc.Get("tooltip.gas_mass", item.unitCount), ignoreEmpty: true);
            if (GasContainer.CanSplit(item))
                builder.AddLine(Loc.Get("tooltip.gas_split"), ignoreEmpty: true);
        }
        if (ProductQuality.Get(item) != 0)
            builder.AddLine(Loc.Get("tooltip.product_quality", ProductQuality.Get(item)), ignoreEmpty: true);

        if (FuelCellMachine.IsCell(item))
        {
            builder.AddLine(Loc.Get("tooltip.fuel_energy", FuelCellMachine.AvailableEnergy(item),
                FuelCellMachine.Reserve(item)), ignoreEmpty: true);
        }

        if (item.IsTag("CHEM_ITEM_TAG"))
        {
            AddChemLine(builder, item, "acid", Loc.Get("chem.acid"));
            AddChemLine(builder, item, "base", Loc.Get("chem.base"));
            AddChemLine(builder, item, "chlorine", Loc.Get("chem.chlorine"));
            AddChemLine(builder, item, "mineral", Loc.Get("chem.mineral"));
            AddChemLine(builder, item, "water", Loc.Get("chem.water"));
            foreach (string liquid in new[] { InorganicReactions.Hcl, InorganicReactions.Nitric,
                InorganicReactions.Caustic, InorganicReactions.Ammonia, InorganicReactions.Nitrate,
                InorganicReactions.Peroxide, InorganicReactions.Hypochlorite })
                AddChemLine(builder, item, liquid, Loc.Get("liquid." + liquid));
        }

        foreach (string hazard in new[] { "TOXIC", "CORROSIVE", "FLAMMABLE", "RADIOACTIVE" })
        {
            if (item.IsTag("ELEM_HAZARD_" + hazard))
            {
                builder.AddLine($"{Loc.Get("tooltip.hazard")}: {Loc.Get("hazard." + hazard.ToLowerInvariant())}", ignoreEmpty: true);
            }
        }

        if (item.IsTag(Ids.ScreenedSeedTag) && !PlantGeneticsHelper.IsScannerOn() && HydroponicHelper.GetPlant(item) != null)
        {
            builder.AddLine(Loc.Get("tooltip.screened"), ignoreEmpty: true);
            var traits = PlantGeneticsHelper.GetTraits(item);
            if (traits.Count == 0)
            {
                builder.AddLine(Loc.Get("tooltip.no_traits"), ignoreEmpty: true);
            }
            else
            {
                foreach (var entry in traits)
                {
                    string label = entry.Item1.maxRank > 1
                        ? $"{entry.Item1.displayName} {PlantGeneticsHelper.RankLabel(entry.Item2)}"
                        : entry.Item1.displayName;
                    builder.AddLine(label, ignoreEmpty: true);
                }
            }
        }
    }

    private static void AddChemLine(RichTextBuilder builder, GameItem item, string liquid, string label)
    {
        string tag = Liquid.GetCurrentPartFromId(liquid);
        if (!item.IsTag(tag))
        {
            return;
        }
        int microlitres = item.GetTagReadonly(tag).GetInt();
        if (microlitres <= 0)
        {
            return;
        }
        builder.AddLine($"{label}: {microlitres / 1000.0:0.##} mL", ignoreEmpty: true);
    }

    private void RegisterDebugFunctions()
    {
        ModHelper.AddDebugFunction(Ids.ModId, ModHelper.DEBUG_CATEGORY_SPAWN,
            Loc.Get("debug.gas_kit"),
            () => SpawnKit(new[]
            {
                Ids.HydrogenCylinder, Ids.OxygenCylinder, "elem_nitrogen_cylinder", "elem_chlorine_cylinder"
            }),
            tall: true);

        ModHelper.AddDebugFunction(Ids.ModId, ModHelper.DEBUG_CATEGORY_SPAWN,
            Loc.Get("debug.machine_kit"),
            () =>
            {
                SpawnKit(ContentDatabase.Machines.Values
                    .Where(machine => ContentDatabase.IsContentEnabled(machine.module, machine.id))
                    .Select(machine => machine.id));
            },
            tall: true);

        ModHelper.AddDebugFunction(Ids.ModId, ModHelper.DEBUG_CATEGORY_SPAWN,
            Loc.Get("debug.material_kit"),
            () =>
            {
                SpawnKit(ContentDatabase.Items.Values
                    .Where(item => item.canSpawnInLoot && ContentDatabase.IsModuleEnabled(item.module))
                    .Select(item => item.id));
            },
            tall: true);
    }

    private static void RegisterStartingScenario()
    {
        if (!ContentDatabase.IsModuleEnabled("WorldIntegration"))
        {
            return;
        }

        ModHelper.AddStartingScenario(
            Ids.ModId,
            "elem_industry_start",
            () => Loc.Get("scenario.elem_industry.title"),
            () => Loc.Get("scenario.elem_industry.desc"),
            new Func<string>[]
            {
                () => Loc.Get("scenario.elem_industry.start")
            },
            () =>
            {
                // Place the laboratory first so it gets priority for inventory space.
                var kit = new List<string> { LaboratoryBay.ItemId };
                if (ContentDatabase.IsModuleEnabled("GasProduction"))
                {
                    kit.Add("elem_electrolysis_cell");
                }
                if (ContentDatabase.IsModuleEnabled("Metallurgy"))
                {
                    kit.Add("elem_rust_extractor");
                    kit.Add(FurnaceChemistry.ModuleId);
                }
                kit.Add("elem_field_manual");
                SpawnKit(kit);
            },
            iconKey: Ids.ModId + ":" + Ids.ElectrolysisCell,
            tag: () => Loc.Get("scenario.elem_industry.tag"));
    }

    private static void SpawnKit(IEnumerable<string> ids)
    {
        foreach (string id in ids)
        {
            GameItem? item = TrySpawn(id);
            if (item != null)
            {
                ModHelper.SetItemOwnerShip(item, true);
                MachineManual.AddToNewMachine(item);
                ModHelper.MoveItemToInventory(item);
            }
        }
    }

    private static GameItem? TrySpawn(string id)
    {
        if (ContentDatabase.Items.TryGetValue(id, out ItemDefinition? definition) &&
            !ContentDatabase.IsModuleEnabled(definition.module))
        {
            return null;
        }
        if (ContentDatabase.Machines.TryGetValue(id, out MachineDefinition? machine) &&
            !ContentDatabase.IsContentEnabled(machine.module, machine.id))
        {
            return null;
        }
        try
        {
            return ModHelper.SpawnItem(id);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[{Ids.ModId}] cannot spawn {id}: {exception.Message}");
            return null;
        }
    }
}
