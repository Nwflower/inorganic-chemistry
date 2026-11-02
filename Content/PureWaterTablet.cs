namespace ElementalIndustry;

internal static class PureWaterTablet
{
    internal const string ItemId = "elem_pure_water_tablet";
    private const string WaterPurificationTabletTag = "WATER_PURIFICATION_TABLET";

    internal static void Configure(GameItem item)
    {
        item.EnableTag(WaterPurificationTabletTag, skipValidate: true);
        item.maySelectSlotItemFunc = (self, _, _) => GeneralHelper.IsItemOwned(self);
        item.canSelectSlotItemFunc = (self, _, _) => GeneralHelper.IsItemOwned(self);
        item.mayThisTargetItemFunc = CanUse;
        item.canThisTargetItemFunc = CanUse;
        item.onThisTargetItemFunc = Use;
    }


    private static bool CanUse(GameItem tablet, GameItem target)
    {
        if (tablet == null || target == null || !GeneralHelper.IsItemOwned(tablet) ||
            !GeneralHelper.IsItemOwned(target) || !target.IsTag("LIQUID_CONTAINER_TAG") || target.unitCount != 1)
        {
            return false;
        }

        string waterTag = Liquid.GetCurrentPartFromId("water");
        return target.GetTagReadonly(waterTag).GetInt() > 0;
    }

    private static void Use(GameItem tablet, GameItem target)
    {
        if (!CanUse(tablet, target)) return;

        WaterHelper.OnWaterPurificationTabletAdded(target);
        WaterHelper.OnWaterPurificationTabletAdded(target);
        ConsumeOne(tablet);
        SpriteHelper.UpdateWaterContainerSprite(target);
    }


    private static void ConsumeOne(GameItem item)
    {
        if (item.unitCount > 1) item.SetUnitCount(item.unitCount - 1);
        else item.Destroy();
    }
}
