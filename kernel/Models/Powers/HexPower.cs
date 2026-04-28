using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Afflictions;

namespace MegaCrit.Sts2.Core.Models.Powers;

public sealed class HexPower : PowerModel
{
	public override PowerType Type => PowerType.Debuff;

	public override PowerStackType StackType => PowerStackType.Single;


	public override void AfterApplied(Creature? applier, CardModel? cardSource)
	{
		foreach (CardModel allCard in base.Owner.Player.PlayerCombatState.AllCards)
		{
			Afflict(allCard);
		}
	}

	public override void AfterCardEnteredCombat(CardModel card)
	{
		if (card.Owner == base.Owner.Player)
		{
			Afflict(card);
		}
	}

	public override void AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
	{
		if (!wasRemovalPrevented && creature == base.Applier)
		{
			PowerCmd.Remove(this);
		}
	}

	public override void AfterRemoved(Creature oldOwner)
	{
		foreach (CardModel allCard in base.Owner.Player.PlayerCombatState.AllCards)
		{
			if (allCard.Affliction is Hexed hexed)
			{
				if (hexed.AppliedEthereal)
				{
					CardCmd.RemoveKeyword(allCard, CardKeyword.Ethereal);
				}
				CardCmd.ClearAffliction(allCard);
			}
		}
		return;
	}

	private void Afflict(CardModel card)
	{
		if (card.Affliction == null)
		{
			Hexed hexed = CardCmd.Afflict<Hexed>(card, base.Amount);
			if (hexed != null && !card.Keywords.Contains(CardKeyword.Ethereal))
			{
				CardCmd.ApplyKeyword(card, CardKeyword.Ethereal);
				hexed.AppliedEthereal = true;
			}
		}
	}
}
