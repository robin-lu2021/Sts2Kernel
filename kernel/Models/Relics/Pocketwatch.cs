using MegaCrit.Sts2.Core;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rooms;

namespace MegaCrit.Sts2.Core.Models.Relics;

public sealed class Pocketwatch : RelicModel
{
	private const string _cardThresholdKey = "CardThreshold";

	private int _cardsPlayedThisTurn;

	private int _cardsPlayedLastTurn;

	public override RelicRarity Rarity => RelicRarity.Rare;



	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlyArray<DynamicVar>(new DynamicVar[2]
	{
		new DynamicVar("CardThreshold", 3m),
		new CardsVar(3)
	});

	public override void AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (cardPlay.Card.Owner != base.Owner)
		{
			return;
		}
		if (!CombatManager.Instance.IsInProgress)
		{
			return;
		}
		_cardsPlayedThisTurn++;
		RefreshCounter();
		return;
	}

	public override decimal ModifyHandDraw(Player player, decimal count)
	{
		if (player.Creature.CombatState.RoundNumber == 1)
		{
			return count;
		}
		if (player != base.Owner)
		{
			return count;
		}
		if ((decimal)_cardsPlayedLastTurn > base.DynamicVars["CardThreshold"].BaseValue)
		{
			return count;
		}
		return count + base.DynamicVars.Cards.BaseValue;
	}

	public override void AfterModifyingHandDraw()
	{
		 
		return;
	}

	public override void BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, CombatState combatState)
	{
		if (side == base.Owner.Creature.Side)
		{
			_cardsPlayedLastTurn = _cardsPlayedThisTurn;
			_cardsPlayedThisTurn = 0;
		}
		return;
	}

	public override void AfterSideTurnStart(CombatSide side, CombatState combatState)
	{
		if (side == base.Owner.Creature.Side)
		{
			RefreshCounter();
		}
		return;
	}

	private void RefreshCounter()
	{
		base.Status = (((decimal)_cardsPlayedThisTurn <= base.DynamicVars["CardThreshold"].BaseValue) ? RelicStatus.Active : RelicStatus.Normal);
		InvokeDisplayAmountChanged();
	}

	public override void AfterCombatEnd(CombatRoom _)
	{
		_cardsPlayedThisTurn = 0;
		_cardsPlayedLastTurn = 0;
		base.Status = RelicStatus.Normal;
		InvokeDisplayAmountChanged();
		return;
	}
}