using System;
using HarmonyLib;
using UnityEngine;

namespace ElementalIndustry;

/// <summary>Gas mass belongs in its tooltip, not on the cylinder sprite.</summary>
internal static class GasOverlayPatch
{
    [HarmonyPatch(typeof(GameItemElement), nameof(GameItemElement.ValidateSelect))]
    private static class NormalCount
    {
        private static void Postfix(GameItemElement __instance, TreeNodeRender ___stackCountText, TreeNodeRender ___stackCountTextBackground)
        {
            if (!GasContainer.IsGasContainer(__instance) || !string.IsNullOrEmpty(__instance.customText)) return;
            ___stackCountText.text.enabled = false;
            ___stackCountTextBackground.text.enabled = false;
        }
    }

    [HarmonyPatch(typeof(GameItemElement), nameof(GameItemElement.UpdateFakeReal))]
    private static class DragCount
    {
        private static void Postfix(GameItemElement __instance, int numPickedUp,
            TreeNodeRender ___stackCountText, TreeNodeRender ___stackCountTextBackground,
            TreeNodeRender ___fakeStackCountText, TreeNodeRender ___fakeStackCountTextBackground)
        {
            if (!GasContainer.IsGasContainer(__instance)) return;
            ___stackCountText.text.enabled = false;
            ___stackCountTextBackground.text.enabled = false;

            // Keep precise splitting possible, with its temporary readout beside
            // the dragged item rather than covering either tank's artwork.
            bool splitting = numPickedUp > 0 && numPickedUp < __instance.unitCount;
            ___fakeStackCountText.text.enabled = splitting;
            ___fakeStackCountTextBackground.text.enabled = splitting;
            if (!splitting) return;
            string mass = numPickedUp + " g";
            ___fakeStackCountText.text.text = mass;
            ___fakeStackCountTextBackground.text.text = mass;
            float rightOfTank = Math.Max(__instance.widthPixels, __instance.heightPixels) + 4f;
            ___fakeStackCountText.rectTransform.anchoredPosition = new Vector2(rightOfTank, 0f);
            ___fakeStackCountTextBackground.rectTransform.anchoredPosition = new Vector2(rightOfTank + 0.5f, -1f);
        }
    }
}
