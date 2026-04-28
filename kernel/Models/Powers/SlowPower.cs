using System.Collections.Generic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core.Models.Powers;

public sealed class SlowPower : PowerModel
{
	private const string _slowAmountKey = "SlowAmount";

	public override PowerType Type => PowerType.Debuff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override int DisplayAmount => base.DynamicVars["SlowAmount"].IntValue * 10;

	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlySingleElementList<DynamicVar>(new DynamicVar("SlowAmount", 0m));

	public override void AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		base.DynamicVars["SlowAmount"].BaseValue++;
		InvokeDisplayAmountChanged();
		return;
	}

	public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		if (target != base.Owner)
		{
			return 1m;
		}
		if (!props.IsPoweredAttack())
		{
			return 1m;
		}
		return 1m + 0.1m * base.DynamicVars["SlowAmount"].BaseValue;
	}

	public override void AfterModifyingDamageAmount(CardModel? cardSource)
	{
		 
		return;
	}

	public override void AfterSideTurnStart(CombatSide side, CombatState combatState)
	{
		if (side != base.Owner.Side)
		{
			return;
		}
		base.DynamicVars["SlowAmount"].BaseValue = 0m;
		InvokeDisplayAmountChanged();
		return;
	}
}
