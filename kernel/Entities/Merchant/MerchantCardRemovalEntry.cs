using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Runs;

namespace MegaCrit.Sts2.Core.Entities.Merchant;

public sealed class MerchantCardRemovalEntry : MerchantEntry
{
	public bool Used { get; private set; }

	public override bool IsStocked => !Used;

	private static int BaseCost => AscensionHelper.GetValueIfAscension(AscensionLevel.Inflation, 100, 75);

	public static int PriceIncrease => AscensionHelper.GetValueIfAscension(AscensionLevel.Inflation, 50, 25);

	public MerchantCardRemovalEntry(Player player)
		: base(player)
	{
		CalcCost();
	}

	public override void CalcCost()
	{
		_cost = BaseCost + PriceIncrease * _player.ExtraFields.CardShopRemovalsUsed;
	}

	public bool OnTryPurchaseWrapper(MerchantInventory? inventory, bool ignoreCost = false, bool cancelable = true)
	{
		if (!base.EnoughGold && !ignoreCost)
		{
			InvokePurchaseFailed(PurchaseStatus.FailureGold);
			return false;
		}
		var (success, goldSpent) = OnTryPurchase(inventory, ignoreCost, cancelable);
		if (success)
		{
			Hook.AfterItemPurchased(_player.RunState, _player, this, goldSpent);
			InvokePurchaseCompleted(this);
		}
		return success;
	}

	protected override (bool, int) OnTryPurchase(MerchantInventory? inventory, bool ignoreCost)
	{
		return OnTryPurchase(inventory, ignoreCost, cancelable: true);
	}

	private (bool, int) OnTryPurchase(MerchantInventory? inventory, bool ignoreCost, bool cancelable)
	{
		if (Used)
		{
			return (false, 0);
		}
		int goldToSpend = ((!ignoreCost) ? base.Cost : 0);
		bool flag = RunManager.Instance.OneOffSynchronizer.DoLocalMerchantCardRemoval(goldToSpend, cancelable).GetAwaiter().GetResult();
		return (flag, goldToSpend);
	}

	protected override void ClearAfterPurchase()
	{
	}

	protected override void RestockAfterPurchase(MerchantInventory? inventory)
	{
	}

	public void SetUsed()
	{
		Used = true;
	}
}
