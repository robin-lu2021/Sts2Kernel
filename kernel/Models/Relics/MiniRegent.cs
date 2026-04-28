using MegaCrit.Sts2.Core;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;

namespace MegaCrit.Sts2.Core.Models.Relics;

public sealed class MiniRegent : RelicModel
{
	private bool _usedThisTurn;

	public override RelicRarity Rarity => RelicRarity.Rare;

	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlySingleElementList<DynamicVar>(new PowerVar<StrengthPower>(1m));


	private bool UsedThisTurn
	{
		get
		{
			return _usedThisTurn;
		}
		set
		{
			AssertMutable();
			_usedThisTurn = value;
		}
	}

	public override void AfterStarsSpent(int amount, Player spender)
	{
		if (spender == base.Owner && !UsedThisTurn)
		{
			UsedThisTurn = true;
			 
			PowerCmd.Apply<StrengthPower>(base.Owner.Creature, base.DynamicVars.Strength.BaseValue, base.Owner.Creature, null);
		}
	}

	public override void BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, CombatState combatState)
	{
		if (side != base.Owner.Creature.Side)
		{
			return;
		}
		UsedThisTurn = false;
		return;
	}

	public override void AfterCombatEnd(CombatRoom _)
	{
		UsedThisTurn = false;
		return;
	}
}