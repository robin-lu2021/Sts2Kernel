using MegaCrit.Sts2.Core;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Rooms;

namespace MegaCrit.Sts2.Core.Models.Relics;

public sealed class MusicBox : RelicModel
{
	private bool _wasUsedThisTurn;

	private CardModel? _cardBeingPlayed;

	public override RelicRarity Rarity => RelicRarity.Ancient;


	private bool WasUsedThisTurn
	{
		get
		{
			return _wasUsedThisTurn;
		}
		set
		{
			AssertMutable();
			_wasUsedThisTurn = value;
		}
	}

	private CardModel? CardBeingPlayed
	{
		get
		{
			return _cardBeingPlayed;
		}
		set
		{
			AssertMutable();
			_cardBeingPlayed = value;
		}
	}

	public override void BeforeCardPlayed(CardPlay cardPlay)
	{
		if (CardBeingPlayed != null)
		{
			return;
		}
		if (cardPlay.Card.Owner != base.Owner)
		{
			return;
		}
		if (WasUsedThisTurn)
		{
			return;
		}
		if (cardPlay.Card.Type != CardType.Attack)
		{
			return;
		}
		CardBeingPlayed = cardPlay.Card;
		return;
	}

	public override void AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (cardPlay.Card == CardBeingPlayed)
		{
			 
			CardModel card = cardPlay.Card.CreateClone();
			CardCmd.ApplyKeyword(card, CardKeyword.Ethereal);
			CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, addedByPlayer: true);
			WasUsedThisTurn = true;
			CardBeingPlayed = null;
		}
	}

	public override void BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, CombatState combatState)
	{
		if (side != base.Owner.Creature.Side)
		{
			return;
		}
		WasUsedThisTurn = false;
		return;
	}

	public override void AfterCombatEnd(CombatRoom _)
	{
		WasUsedThisTurn = false;
		return;
	}
}