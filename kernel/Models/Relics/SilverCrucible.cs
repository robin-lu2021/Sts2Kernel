using MegaCrit.Sts2.Core;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace MegaCrit.Sts2.Core.Models.Relics;

public sealed class SilverCrucible : RelicModel
{
	private int _timesUsed;

	private int _treasureRoomsEntered;

	public override RelicRarity Rarity => RelicRarity.Ancient;

	public override bool IsUsedUp
	{
		get
		{
			if (TimesUsed >= base.DynamicVars.Cards.IntValue)
			{
				return TreasureRoomsEntered > 0;
			}
			return false;
		}
	}

	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlySingleElementList<DynamicVar>(new CardsVar(3));

	[SavedProperty]
	public int TimesUsed
	{
		get
		{
			return _timesUsed;
		}
		set
		{
			AssertMutable();
			_timesUsed = value;
			InvokeDisplayAmountChanged();
			CheckIfUsedUp();
		}
	}

	[SavedProperty]
	public int TreasureRoomsEntered
	{
		get
		{
			return _treasureRoomsEntered;
		}
		set
		{
			AssertMutable();
			_treasureRoomsEntered = value;
			CheckIfUsedUp();
		}
	}

	public override bool IsAllowed(IRunState runState)
	{
		return runState.Players.Count == 1;
	}

	private void CheckIfUsedUp()
	{
		if (IsUsedUp)
		{
			base.Status = RelicStatus.Disabled;
		}
	}

	public override bool TryModifyCardRewardOptionsLate(Player player, List<CardCreationResult> cardRewards, CardCreationOptions options)
	{
		if (player != base.Owner)
		{
			return false;
		}
		if (TimesUsed >= base.DynamicVars.Cards.IntValue)
		{
			return false;
		}
		foreach (CardCreationResult cardReward in cardRewards)
		{
			CardModel card = cardReward.Card;
			if (card.IsUpgradable)
			{
				CardModel card2 = base.Owner.RunState.CloneCard(card);
				CardCmd.Upgrade(card2);
				cardReward.ModifyCard(card2, this);
			}
		}
		return true;
	}

	public override void AfterModifyingCardRewardOptions()
	{
		if (TimesUsed >= base.DynamicVars.Cards.IntValue)
		{
			return;
		}
		TimesUsed++;
		return;
	}

	public override void AfterRoomEntered(AbstractRoom room)
	{
		if (room is TreasureRoom)
		{
			TreasureRoomsEntered++;
		}
		return;
	}

	public override bool ShouldGenerateTreasure(Player player)
	{
		if (player != base.Owner)
		{
			return true;
		}
		return TreasureRoomsEntered > 1;
	}
}