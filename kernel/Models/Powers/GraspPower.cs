using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models.Afflictions;

namespace MegaCrit.Sts2.Core.Models.Powers;

public sealed class GraspPower : PowerModel
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Single;

	public override void AfterApplied(Creature? applier, CardModel? cardSource)
	{
		foreach (Creature item in base.Owner.CombatState.Allies.ToList())
		{
			if (!item.IsPlayer)
			{
				continue;
			}
			List<CardModel> list = item.Player.PlayerCombatState.AllCards.ToList();
			foreach (CardModel item2 in list)
			{
				Afflict(item2);
			}
		}
	}

	public override void AfterCardEnteredCombat(CardModel card)
	{
		if (card.Affliction == null)
		{
			Afflict(card);
		}
	}

	public override void AfterRemoved(Creature oldOwner)
	{
		if (oldOwner.CombatState == null)
		{
			return;
		}
		foreach (Creature item in oldOwner.CombatState.Allies.ToList())
		{
			if (!item.IsPlayer)
			{
				continue;
			}
			List<CardModel> list = item.Player.PlayerCombatState.AllCards.Where((CardModel c) => c.Affliction is Weighted).ToList();
			foreach (CardModel item2 in list)
			{
				CardCmd.ClearAffliction(item2);
			}
		}
		return;
	}

	private void Afflict(CardModel card)
	{
		if (card.Affliction == null)
		{
			CardCmd.Afflict<Weighted>(card, base.Amount);
		}
	}
}
