using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ElementalIndustry;

/// <summary>
/// An after-hours merchant with its own counter; it never adds goods to the inventor.
/// The counter and the leave/return handling are the game's own.
/// </summary>
internal sealed class ChemistShopLocation : AfterhourLocation
{
    internal const string LocationId = "elemental_industry.chemist";

    public override string Id => LocationId;
    public override string Source => Ids.ModId;
    public override string ImageKey => "dr_jackson";
    public override string AmbienceId => "dr_jackson";
    // Reuses the Dr. Jackson stage and its counter layout; the portrait is part of that
    // vanilla scene, so this shop does not claim a dedicated background yet.
    public override GameObject StageObject => MapUIManager.Instance?.upgradeStoreScene!;
    public override bool ShowsStoreCounters => true;
    public override string GetTitle() => Loc.Get("shop.chemist.title");
    public override string GetLeaveWarning() => Loc.Get("shop.chemist.leave_warning");
    public override bool IsDiscovered() => ContentDatabase.IsModuleEnabled("WorldIntegration");
    public override bool IsAvailable() => ContentDatabase.IsModuleEnabled("WorldIntegration") && base.IsAvailable();

    public override Vector2? GetFrontCounterPosition(int width, int height) =>
        new DrJacksonLocation().GetFrontCounterPosition(width, height);

    public override Vector2? GetBackCounterPosition(int width, int height) =>
        new DrJacksonLocation().GetBackCounterPosition(width, height);

    public override void Arrive()
    {
        EmporiumEntry entry = EmporiumEntry.Instance;
        // Never clear an existing counter to make room for merchandise.
        if (entry.frontInvinvElement.childItems.Count != 0 ||
            entry.backInvinvElement.childItems.Count != 0 ||
            entry.backInvinvElementCounter.childItems.Count != 0)
        {
            Debug.LogWarning($"[{Ids.ModId}] chemist stock skipped: a counter is not empty");
            return;
        }

        entry.frontInvWindow.Show();
        entry.backInvWindow.Show();
        MapUIManager.Instance.TransferBackInvToCounter();
        entry.ExpandFrontCounterGrid();

        var client = new StoreClient
        {
            identifier = "elemental_industry_chemist",
            displayName = GetTitle(),
            spriteName = SpriteDict.GetRetiredChemistSpriteName(),
            clientIntent = StoreClient.ClientIntent.SELLNBUY,
            isIntroduced = true,
            useClientBudget = false,
            nonShootable = true
        };
        ModHelper.SetClientSource(client, Ids.ModId);
        PlayerStore.Instance.currentClientInstance = StoreClientInstance.CreateClientInstance(client);
        PlayerStore.Instance.isClientArrived = true;
        // Do not call ActivateClientOutsideStore: it hardcodes the inventor.
        StoreUIManager.Instance.HideClient();
        StoreUIManager.Instance.SetClientPositionToDrJackson();
        MapUIManager.Instance.SetCreditDisplayPositionToDrJackson();

        int day = StoreStation.Instance.dayCounter;
        foreach (ChemistOffer offer in ChemistStockPlan.ForDay(BuildCandidates(), day))
        {
            TryOffer(offer, entry.frontInvinvElement);
        }
    }

    /// <summary>Everything this shop may sell while the relevant modules are enabled.</summary>
    internal static List<ChemistOffer> BuildCandidates()
    {
        var machines = ContentDatabase.Machines.Values
            .Where(machine => ContentDatabase.IsContentEnabled(machine.module, machine.id))
            .Select(machine => machine.id);
        var chemicals = ContentDatabase.Items.Values
            .Where(item => ContentDatabase.IsModuleEnabled(item.module) &&
                (item.id == FurnaceChemistry.ModuleId || item.module == "ChemicalProcessing" ||
                 item.itemTypes.Contains("CHEMICAL_SUPPLIES") ||
                 item.itemTypes.Contains("MEDICAL_SUPPLIES")))
            .Select(item => item.id);
        return ChemistStockPlan.Build(machines, chemicals);
    }

    private void TryOffer(ChemistOffer offer, GameGridInventory front)
    {
        if (!front.CanTakeAnything())
        {
            return;
        }
        GameItem? item = null;
        try
        {
            item = offer.Kind switch
            {
                ChemistOfferKind.AcidBottle => PreBuildChemHelper.CreateAcidBottle(),
                ChemistOfferKind.BaseBottle => PreBuildChemHelper.CreateBaseBottle(),
                ChemistOfferKind.ReducingBottle => PreBuildChemHelper.CreateReducingAgentBottle(),
                _ => DirectoryMaster.Item(offer.Identifier)
            };
            if (item == null || item.parentInventory != null)
            {
                return;
            }
            MachineManual.AddToNewMachine(item);
            GeneralHelper.SetItemOwned(item, false);
            SlotMarker slot = front.TryFindOneValidInventorySlot(item);
            // The vanilla search stacks first; refuse that path so an existing item is untouched.
            if ((item.unitCount != 1 && !GasContainer.IsGasContainer(item)) || slot == null || slot.inventory != front ||
                slot.targetItem != null || slot.numTransfer < item.unitCount)
            {
                item.Destroy();
                return;
            }
            if (!PlayerStore.Instance.TryAddDirectSellingItemToTable(item))
            {
                return; // the game reports and cleans a skipped offer
            }
        }
        catch (Exception exception)
        {
            if (item != null && item.parentInventory == null &&
                (!(item is GameItemElement element) || !element.IsDestroyed()))
            {
                item.Destroy();
            }
            Debug.LogError($"[{Ids.ModId}] chemist could not offer {offer.Key}: {exception}");
        }
    }

    public override void Leave()
    {
        // The vanilla leave returns owned counter items to the player and destroys the
        // remaining unsold (unowned) offers, then resets the counter and the client UI.
        new DrJacksonLocation().Leave();
    }
}
