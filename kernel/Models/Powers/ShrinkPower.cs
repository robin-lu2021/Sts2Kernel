using System.Collections.Generic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core.Models.Powers;

public sealed class ShrinkPower : PowerModel
{
	public const decimal damageDecrease = 30m;

	private const string _damageDecreaseKey = "DamageDecrease";

	private const string _applierNameKey = "ApplierName";

	public override PowerType Type => PowerType.Debuff;

	public override PowerStackType StackType
	{
		get
		{
			if (!IsInfinite)
			{
				return PowerStackType.Counter;
			}
			return PowerStackType.Single;
		}
	}

	public override bool AllowNegative => true;

	private bool IsInfinite => base.Amount < 0;

	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlyArray<DynamicVar>(new DynamicVar[2]
	{
		new DynamicVar("DamageDecrease", 30m),
		new StringVar("ApplierName")
	});

	public override void AfterApplied(Creature? applier, CardModel? cardSource)
	{
		Creature applier2 = base.Applier;
		if (applier2 != null && applier2.IsMonster)
		{
			((StringVar)base.DynamicVars["ApplierName"]).StringValue = base.Applier.Monster.Title.GetFormattedText();
		}
		return;
	}

	public override void AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
	{
		if (!IsInfinite && side == base.Owner.Side)
		{
			PowerCmd.Decrement(this);
		}
	}

	public override void AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
	{
		if (!wasRemovalPrevented && creature == base.Applier)
		{
			PowerCmd.Remove(this);
		}
	}

	public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		if (base.Owner != dealer)
		{
			return 1m;
		}
		if (!props.IsPoweredAttack())
		{
			return 1m;
		}
		return (100m - base.DynamicVars["DamageDecrease"].BaseValue) / 100m;
	}
}
