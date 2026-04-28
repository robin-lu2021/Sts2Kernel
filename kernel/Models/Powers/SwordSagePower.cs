using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Cards;

namespace MegaCrit.Sts2.Core.Models.Powers;

public sealed class SwordSagePower : PowerModel
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;


	public override void AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
	{
		if (!(power is SwordSagePower))
		{
			return;
		}
		if (power.Owner != base.Owner)
		{
			return;
		}
		IEnumerable<CardModel> enumerable = base.Owner.Player?.PlayerCombatState?.AllCards ?? Array.Empty<CardModel>();
		foreach (CardModel item in enumerable)
		{
			if (item is SovereignBlade sovereignBlade)
			{
				sovereignBlade.SetRepeats(base.Amount + 1);
			}
		}
		return;
	}

	public override void AfterCardEnteredCombat(CardModel card)
	{
		if (card.Owner != base.Owner.Player)
		{
			return;
		}
		if (!(card is SovereignBlade sovereignBlade))
		{
			return;
		}
		sovereignBlade.SetRepeats(base.Amount + 1);
		return;
	}

	public override void AfterRemoved(Creature oldOwner)
	{
		IEnumerable<CardModel> enumerable = oldOwner.Player?.PlayerCombatState?.AllCards ?? Array.Empty<CardModel>();
		foreach (CardModel item in enumerable)
		{
			if (item is SovereignBlade sovereignBlade)
			{
				sovereignBlade.SetRepeats(1m);
			}
		}
		return;
	}
}
