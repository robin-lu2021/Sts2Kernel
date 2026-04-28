using MegaCrit.Sts2.Core;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core.Models.Relics;

public sealed class LavaLamp : RelicModel
{
	private bool _tookDamageThisCombat;

	public override RelicRarity Rarity => RelicRarity.Shop;

	[SavedProperty]
	public bool TookDamageThisCombat
	{
		get
		{
			return _tookDamageThisCombat;
		}
		set
		{
			AssertMutable();
			_tookDamageThisCombat = value;
		}
	}

	public override void AfterRoomEntered(AbstractRoom room)
	{
		TookDamageThisCombat = false;
		return;
	}

	public override void AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		if (!(base.Owner.RunState.CurrentRoom is CombatRoom))
		{
			return;
		}
		if (target != base.Owner.Creature)
		{
			return;
		}
		if (result.UnblockedDamage <= 0)
		{
			return;
		}
		if (props.HasFlag(ValueProp.Unblockable))
		{
			return;
		}
		TookDamageThisCombat = true;
		return;
	}

	public override bool TryModifyCardRewardOptionsLate(Player player, List<CardCreationResult> cardRewards, CardCreationOptions options)
	{
		if (!(base.Owner.RunState.CurrentRoom is CombatRoom))
		{
			return false;
		}
		if (player != base.Owner)
		{
			return false;
		}
		if (TookDamageThisCombat)
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
}