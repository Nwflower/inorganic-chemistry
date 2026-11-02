using System;

namespace ElementalIndustry;

[Serializable]
public sealed class ModuleDefinition
{
    public string id = "";
    public string nameKey = "";
    public bool defaultEnabled = true;
    public string[] dependsOn = Array.Empty<string>();
}

[Serializable]
public sealed class ModuleFile
{
    public ModuleDefinition[] modules = Array.Empty<ModuleDefinition>();
}

[Serializable]
public sealed class BuildingDefinition
{
    public string id = "";
    public bool defaultEnabled = true;
}

[Serializable]
public sealed class BuildingFile
{
    public BuildingDefinition[] buildings = Array.Empty<BuildingDefinition>();
}

[Serializable]
public sealed class ItemDefinition
{
    public string id = "";
    public string module = "Core";
    public string sprite = "";
    public string copySpriteFrom = "";
    public long value = 10;
    public string[] itemTypes = Array.Empty<string>();
    public string[] tags = Array.Empty<string>();
    public string[] hazards = Array.Empty<string>();
    public string gas = "";
    public bool stackable = false;
    public string chemLiquid = "";
    public int chemML = 0;
    public int chemWaterML = 0;
    public bool soluble = false;
    public bool blendable = false;
    public string chemicalProductIngredient = "";
    // Native finisher volumes are microlitres, unlike chemML above.
    public string finisherIngredient = "";
    public int finisherVolume = 0;
    public int finisherMinIngredient = 0;
    public int finisherMinWater = 0;
    public bool finisherDry = false;
    public bool ingot = false;
    public bool canSpawnInLoot = false;
}

[Serializable]
public sealed class ItemFile
{
    public ItemDefinition[] items = Array.Empty<ItemDefinition>();
}

[Serializable]
public sealed class MachineDefinition
{
    public string id = "";
    public string module = "Core";
    public string archetype = "processor";
    public string sprite = "";
    public string copySpriteFrom = "";
    public long value = 500;
    public int power = 0;
    /// <summary>Native machinery divides the effect of efficiency modules by this ratio.</summary>
    public int efficiencyRatio = 1;
    public int inputWidth = 2;
    public int inputHeight = 2;
    public int outputWidth = 2;
    public int outputHeight = 2;
    public bool waterSlot = false;
    public bool batterySlot = true;
    public bool moduleBay = false;
    public bool outputSlot = true;
    public string[] recipeIds = Array.Empty<string>();
}

[Serializable]
public sealed class MachineFile
{
    public MachineDefinition[] machines = Array.Empty<MachineDefinition>();
}

[Serializable]
public sealed class ItemAmountDefinition
{
    public string id = "";
    public string tag = "";
    public int count = 1;
    /// <summary>Optional ingot purity the input must already have (e.g. INGOT_PURITY_LOW).</summary>
    public string purity = "";
    public bool minimumPurity = false;
    public bool maximumPurity = false;
    /// <summary>When positive, one matching item loses this much durability instead of being consumed.</summary>
    public int durability = 0;
}

[Serializable]
public sealed class OutputAmountDefinition
{
    public string id = "";
    public int count = 1;
    public string purity = "";
    /// <summary>When set, each unit is rolled from this weighted pool instead of <see cref="id"/>.</summary>
    public RollEntryDefinition[] roll = Array.Empty<RollEntryDefinition>();
}

[Serializable]
public sealed class RollEntryDefinition
{
    /// <summary>Spawn key; native <c>random_*</c> keys also roll shape and effects.</summary>
    public string spawn = "";
    /// <summary>Identifier of the spawned item, used by the output-grid filter.</summary>
    public string id = "";
    public int weight = 1;
    /// <summary>Native base value of the plain module; value-roll recipes pick entries by it.</summary>
    public long value = 0;
}

/// <summary>
/// Picks outputs by input value: every accepted material in the input grid is used, the
/// floor is total value + <see cref="floorBonus"/>, the ceiling weighs the key material
/// and the others separately.
/// </summary>
[Serializable]
public sealed class ValueRollDefinition
{
    public string key = "";
    public string[] materials = Array.Empty<string>();
    public int keyMultiplier = 4;
    public int otherMultiplier = 2;
    public int floorBonus = 10;
    public int ceilingBonus = 30;
    /// <summary>Largest base-stat boost, in percent of module value, used to absorb surplus.</summary>
    public int maxStatBonus = 50;
}

[Serializable]
public sealed class RecipeDefinition
{
    public string id = "";
    public string machine = "";
    public string kind = "process";
    public int power = 0;
    public int water = 0;
    public string waterContaminant = "";
    public ItemAmountDefinition[] inputs = Array.Empty<ItemAmountDefinition>();
    public OutputAmountDefinition[] outputs = Array.Empty<OutputAmountDefinition>();
    public ValueRollDefinition? valueRoll;
}

[Serializable]
public sealed class RecipeFile
{
    public RecipeDefinition[] recipes = Array.Empty<RecipeDefinition>();
}

[Serializable]
public sealed class LootPatchDefinition
{
    public string table = "";
    public ItemAmountDefinition[] add = Array.Empty<ItemAmountDefinition>();
    public float weight = 1f;
}

[Serializable]
public sealed class LootPatchFile
{
    public LootPatchDefinition[] patches = Array.Empty<LootPatchDefinition>();
}
