using MegaCrit.Sts2.Core;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.TestSupport;

namespace MegaCrit.Sts2.Core.Models.Relics;

public sealed class LordsParasol : RelicModel
{
	public override RelicRarity Rarity => RelicRarity.Ancient;

	public override void AfterRoomEntered(AbstractRoom room)
	{
		if (!(room is MerchantRoom merchantRoom))
		{
			return;
		}
		PurchaseEverything(merchantRoom.Inventory);
		return;
	}

	private void PurchaseEverything(MerchantInventory inventory)
	{
		if (inventory.Player != base.Owner)
		{
			return;
		}
		bool uiBlocked = false;
		try
		{
			foreach (MerchantCardEntry characterCardEntry in inventory.CharacterCardEntries)
			{
				characterCardEntry.OnTryPurchaseWrapper(inventory, ignoreCost: true);
			}
			foreach (MerchantCardEntry colorlessCardEntry in inventory.ColorlessCardEntries)
			{
				colorlessCardEntry.OnTryPurchaseWrapper(inventory, ignoreCost: true);
			}
			foreach (MerchantRelicEntry relicEntry in inventory.RelicEntries)
			{
				NRun.Instance.GlobalUi.TopBar.Map.Enable();
				NRun.Instance.GlobalUi.TopBar.Deck.Enable();
				relicEntry.OnTryPurchaseWrapper(inventory, ignoreCost: true);
				NRun.Instance.GlobalUi.TopBar.Deck.Disable();
				NRun.Instance.GlobalUi.TopBar.Map.Disable();
			}
			foreach (MerchantPotionEntry potionEntry in inventory.PotionEntries)
			{
				potionEntry.OnTryPurchaseWrapper(inventory, ignoreCost: true);
			}
		}
		finally
		{
			;
		}
		if (inventory.CardRemovalEntry != null)
		{
			inventory.CardRemovalEntry.OnTryPurchaseWrapper(inventory, ignoreCost: true, cancelable: false);
		}
	}
}
