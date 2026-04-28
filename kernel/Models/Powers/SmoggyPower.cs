using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Afflictions;

namespace MegaCrit.Sts2.Core.Models.Powers;

public sealed class SmoggyPower : PowerModel
{
	public override PowerType Type => PowerType.Debuff;

	public override PowerStackType StackType => PowerStackType.Single;

	public override void AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (cardPlay.Card.Owner.Creature != base.Owner || cardPlay.Card.Type != CardType.Skill)
		{
			return;
		}
		 
		IEnumerable<CardModel> allCards = base.Owner.Player.PlayerCombatState.AllCards;
		foreach (CardModel item in allCards)
		{
			if (item.Type == CardType.Skill && item.Affliction == null)
			{
				CardCmd.Afflict<Smog>(item, 1m);
			}
		}
	}

	public override void AfterCardEnteredCombat(CardModel card)
	{
		if (card.Owner == base.Owner.Player && card.Affliction == null && card.Type == CardType.Skill && CombatManager.Instance.History.CardPlaysStarted.Any((CardPlayStartedEntry e) => e.HappenedThisTurn(base.CombatState) && e.CardPlay.Card.Type == CardType.Skill && e.CardPlay.Card.Owner.Creature == base.Owner))
		{
			CardCmd.Afflict<Smog>(card, 1m);
		}
	}

	public override void AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
	{
		if (side != base.Owner.Side)
		{
			return;
		}
		IEnumerable<CardModel> enumerable = base.Owner.Player?.PlayerCombatState?.AllCards ?? Array.Empty<CardModel>();
		foreach (CardModel item in enumerable)
		{
			if (item.Affliction is Smog)
			{
				CardCmd.ClearAffliction(item);
			}
		}
		return;
	}

	public override bool ShouldPlay(CardModel card, AutoPlayType _)
	{
		if (card.Owner != base.Owner.Player)
		{
			return true;
		}
		return !(card.Affliction is Smog);
	}
}
