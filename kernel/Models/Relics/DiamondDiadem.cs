using MegaCrit.Sts2.Core;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;

namespace MegaCrit.Sts2.Core.Models.Relics;

public sealed class DiamondDiadem : RelicModel
{
	private const string _cardThresholdKey = "CardThreshold";

	private int _cardsPlayedThisTurn;

	public override RelicRarity Rarity => RelicRarity.Ancient;

	public int CardsPlayedThisTurn
	{
		get
		{
			return _cardsPlayedThisTurn;
		}
		set
		{
			AssertMutable();
			_cardsPlayedThisTurn = value;
		}
	}

	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlySingleElementList<DynamicVar>(new DynamicVar("CardThreshold", 2m));

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
		CardsPlayedThisTurn++;
		RefreshCounter();
		return;
	}

	public override void BeforeTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
	{
		if (side == base.Owner.Creature.Side && !((decimal)CardsPlayedThisTurn > base.DynamicVars["CardThreshold"].BaseValue))
		{
			PowerCmd.Apply<DiamondDiademPower>(base.Owner.Creature, 1m, base.Owner.Creature, null);
		}
	}

	public override void AfterSideTurnStart(CombatSide side, CombatState combatState)
	{
		if (side == base.Owner.Creature.Side)
		{
			CardsPlayedThisTurn = 0;
			RefreshCounter();
		}
		return;
	}

	private void RefreshCounter()
	{
		base.Status = (((decimal)CardsPlayedThisTurn <= base.DynamicVars["CardThreshold"].BaseValue) ? RelicStatus.Active : RelicStatus.Normal);
		InvokeDisplayAmountChanged();
	}

	public override void AfterCombatEnd(CombatRoom _)
	{
		CardsPlayedThisTurn = 0;
		base.Status = RelicStatus.Normal;
		InvokeDisplayAmountChanged();
		return;
	}
}