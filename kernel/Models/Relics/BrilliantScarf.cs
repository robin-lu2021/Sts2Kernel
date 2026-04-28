using MegaCrit.Sts2.Core;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rooms;

namespace MegaCrit.Sts2.Core.Models.Relics;

public sealed class BrilliantScarf : RelicModel
{
	private int _cardsPlayedThisTurn;

	public override RelicRarity Rarity => RelicRarity.Ancient;



	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlySingleElementList<DynamicVar>(new CardsVar(5));

	private int CardsPlayedThisTurn
	{
		get
		{
			return _cardsPlayedThisTurn;
		}
		set
		{
			AssertMutable();
			_cardsPlayedThisTurn = value;
			UpdateDisplay();
		}
	}

	private void UpdateDisplay()
	{
		int intValue = base.DynamicVars.Cards.IntValue;
		base.Status = ((CardsPlayedThisTurn == intValue - 1) ? RelicStatus.Active : RelicStatus.Normal);
		InvokeDisplayAmountChanged();
	}

	public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
	{
		modifiedCost = originalCost;
		if (!ShouldModifyCost(card))
		{
			return false;
		}
		modifiedCost = default(decimal);
		return true;
	}

	public override bool TryModifyStarCost(CardModel card, decimal originalCost, out decimal modifiedCost)
	{
		modifiedCost = originalCost;
		if (!ShouldModifyCost(card))
		{
			return false;
		}
		modifiedCost = default(decimal);
		return true;
	}

	public override void BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, CombatState combatState)
	{
		if (side != base.Owner.Creature.Side)
		{
			return;
		}
		CardsPlayedThisTurn = 0;
		return;
	}

	public override void AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (!CombatManager.Instance.IsInProgress)
		{
			return;
		}
		if (cardPlay.IsAutoPlay)
		{
			return;
		}
		if (cardPlay.Card.Owner != base.Owner)
		{
			return;
		}
		CardsPlayedThisTurn++;
		return;
	}

	public override void AfterCombatEnd(CombatRoom _)
	{
		CardsPlayedThisTurn = 0;
		return;
	}

	private bool ShouldModifyCost(CardModel card)
	{
		if (!CombatManager.Instance.IsInProgress)
		{
			return false;
		}
		if (card.Owner.Creature != base.Owner.Creature)
		{
			return false;
		}
		if ((decimal)CardsPlayedThisTurn != base.DynamicVars.Cards.BaseValue - 1m)
		{
			return false;
		}
		bool flag;
		switch (card.Pile?.Type)
		{
		case PileType.Hand:
		case PileType.Play:
			flag = true;
			break;
		default:
			flag = false;
			break;
		}
		if (!flag)
		{
			return false;
		}
		return true;
	}
}