using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Afflictions;

namespace MegaCrit.Sts2.Core.Models.Powers;

public sealed class RingingPower : PowerModel
{
	public override PowerType Type => PowerType.Debuff;

	public override PowerStackType StackType => PowerStackType.Single;

	public override void AfterApplied(Creature? applier, CardModel? cardSource)
	{
		IEnumerable<CardModel> allCards = base.Owner.Player.PlayerCombatState.AllCards;
		foreach (CardModel item in allCards)
		{
			if (item.Affliction == null)
			{
				CardCmd.Afflict<Ringing>(item, 1m);
			}
		}
	}

	public override void AfterCardEnteredCombat(CardModel card)
	{
		if (card.Owner == base.Owner.Player && card.Affliction == null)
		{
			CardCmd.Afflict<Ringing>(card, 1m);
		}
	}

	public override void AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
	{
		if (side == base.Owner.Side)
		{
			 
			PowerCmd.Remove(this);
		}
	}

	public override void AfterRemoved(Creature oldOwner)
	{
		IEnumerable<CardModel> enumerable = oldOwner.Player?.PlayerCombatState?.AllCards ?? Array.Empty<CardModel>();
		foreach (CardModel item in enumerable)
		{
			if (item.Affliction is Ringing)
			{
				CardCmd.ClearAffliction(item);
			}
		}
		return;
	}

	public override bool ShouldPlay(CardModel card, AutoPlayType _)
	{
		if (card.Owner.Creature != base.Owner)
		{
			return true;
		}
		if (!(card.Affliction is Ringing))
		{
			return true;
		}
		return !CombatManager.Instance.History.CardPlaysStarted.Any((CardPlayStartedEntry e) => e.HappenedThisTurn(base.CombatState) && e.CardPlay.Card.Owner.Creature == base.Owner);
	}
}
