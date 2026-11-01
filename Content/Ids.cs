namespace ElementalIndustry;

/// <summary>
/// Stable ids/tags for the original gas slice. Keep these values forever: they are
/// written into save files. New content uses data ids and does not migrate these values.
/// </summary>
internal static class Ids
{
    public const string ModId = "elemental_industry";
    public const string ModName = "Inorganic Chemistry Basics";

    public const string HydrogenCylinder = "elem_hydrogen_cylinder";
    public const string OxygenCylinder = "elem_oxygen_cylinder";

    public const string ElectrolysisCell = "elem_electrolysis_cell";
    public const string FuelCell = "elem_hydrogen_fuel_cell";
    public const string FuelCellTop = "elem_hydrogen_fuel_cell_top";

    // Gas stacks carry one gas type; their unit count is the mass in grams.
    public const string GasContainerTag = "GAS_CONTAINER_TAG";
    public const string GasTypeTag = "GAS_TYPE";

    // Set on a seed after it has been screened with an oxygen cylinder.
    public const string ScreenedSeedTag = "ELEM_SEED_SCREENED_TAG";
}
