using MegaCrit.Sts2.Core;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rooms;

namespace MegaCrit.Sts2.Core.Models.Relics;

public sealed class ArtOfWar : RelicModel
{
	private bool _anyAttacksPlayedLastTurn;

	private bool _anyAttacksPlayedThisTurn;

	public override RelicRarity Rarity => RelicRarity.Rare;

	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlySingleElementList<DynamicVar>(new EnergyVar(1));

	private bool AnyAttacksPlayedLastTurn
	{
		get
		{
			return _anyAttacksPlayedLastTurn;
		}
		set
		{
			AssertMutable();
			_anyAttacksPlayedLastTurn = value;
		}
	}

	private bool AnyAttacksPlayedThisTurn
	{
		get
		{
			return _anyAttacksPlayedThisTurn;
		}
		set
		{
			AssertMutable();
			_anyAttacksPlayedThisTurn = value;
		}
	}


	public override void AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (base.Owner != cardPlay.Card.Owner)
		{
			return;
		}
		if (!CombatManager.Instance.IsInProgress)
		{
			return;
		}
		if (cardPlay.Card.Type != CardType.Attack)
		{
			return;
		}
		if (AnyAttacksPlayedLastTurn)
		{
			return;
		}
		base.Status = RelicStatus.Normal;
		AnyAttacksPlayedThisTurn = true;
		return;
	}

	public override void AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
	{
		if (side != base.Owner.Creature.Side)
		{
			return;
		}
		AnyAttacksPlayedLastTurn = AnyAttacksPlayedThisTurn;
		AnyAttacksPlayedThisTurn = false;
		return;
	}

	public override void AfterEnergyReset(Player player)
	{
		if (player != base.Owner)
		{
			return;
		}
		base.Status = RelicStatus.Active;
		if (base.Owner.Creature.CombatState.RoundNumber > 1)
		{
			if (!AnyAttacksPlayedLastTurn)
			{
				 
				PlayerCmd.GainEnergy(base.DynamicVars.Energy.BaseValue, base.Owner);
			}
			AnyAttacksPlayedLastTurn = false;
			AnyAttacksPlayedThisTurn = false;
		}
	}

	public override void AfterCombatEnd(CombatRoom _)
	{
		base.Status = RelicStatus.Normal;
		AnyAttacksPlayedLastTurn = false;
		AnyAttacksPlayedThisTurn = false;
		return;
	}
}