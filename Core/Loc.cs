namespace ElementalIndustry;

internal static class Loc
{
    public static string Get(string key)
    {
        return ModHelper.GetLocalized(Ids.ModId, key);
    }

    public static string Get(string key, params object[] args)
    {
        return ModHelper.GetLocalized(Ids.ModId, key, args);
    }
}
