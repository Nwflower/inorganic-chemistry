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
    public bool soluble = false;
    public bool blendable = false;
    public string chemicalProductIngredient = "";
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
}

[Serializable]
public sealed class OutputAmountDefinition
{
    public string id = "";
    public int count = 1;
    public string purity = "";
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
