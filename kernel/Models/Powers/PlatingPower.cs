using System.Collections.Generic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core.Models.Powers;

public sealed class PlatingPower : PowerModel
{
	private const string _decrementKey = "Decrement";

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override bool ShouldScaleInMultiplayer => true;


	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlySingleElementList<DynamicVar>(new DynamicVar("Decrement", 1m));

	public override void AfterApplied(Creature? applier, CardModel? cardSource)
	{
		if (base.Owner.Side == CombatSide.Enemy)
		{
			base.DynamicVars["Decrement"].BaseValue = base.Owner.CombatState.RunState.Players.Count;
		}
		return;
	}

	public override void BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, CombatState combatState)
	{
		if (side != CombatSide.Player)
		{
			return;
		}
		if (base.Owner.IsPlayer)
		{
			return;
		}
		if (combatState.RoundNumber != 1)
		{
			return;
		}
		CreatureCmd.GainBlock(base.Owner, base.Amount, ValueProp.Unpowered, null);
		return;
	}

	public override void BeforeTurnEndEarly(PlayerChoiceContext choiceContext, CombatSide side)
	{
		if (side == base.Owner.Side)
		{
			CreatureCmd.GainBlock(base.Owner, base.Amount, ValueProp.Unpowered, null);
		}
	}

	public override void AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
	{
		if (side == CombatSide.Enemy)
		{
			if (base.Owner.Side == CombatSide.Enemy)
			{
				PowerCmd.ModifyAmount(this, -base.DynamicVars["Decrement"].BaseValue, null, null);
			}
			else
			{
				PowerCmd.Decrement(this);
			}
		}
	}
}
