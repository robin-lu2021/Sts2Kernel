using MegaCrit.Sts2.Core;
using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core.Models.Relics;

public sealed class BeatingRemnant : RelicModel
{
	private const string _maxHpLossKey = "MaxHpLoss";

	private decimal _damageReceivedThisTurn;

	public override RelicRarity Rarity => RelicRarity.Rare;

	private decimal DamageReceivedThisTurn
	{
		get
		{
			return _damageReceivedThisTurn;
		}
		set
		{
			AssertMutable();
			_damageReceivedThisTurn = value;
		}
	}

	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlySingleElementList<DynamicVar>(new DynamicVar("MaxHpLoss", 20m));

	public override decimal ModifyHpLostAfterOsty(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		if (!CombatManager.Instance.IsInProgress)
		{
			return amount;
		}
		if (target != base.Owner.Creature)
		{
			return amount;
		}
		return Math.Min(amount, base.DynamicVars["MaxHpLoss"].BaseValue - DamageReceivedThisTurn);
	}

	public override void AfterModifyingHpLostAfterOsty()
	{
		 
		return;
	}

	public override void AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		if (!CombatManager.Instance.IsInProgress)
		{
			return;
		}
		if (target != base.Owner.Creature)
		{
			return;
		}
		DamageReceivedThisTurn += (decimal)result.UnblockedDamage;
		return;
	}

	public override void BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, CombatState combatState)
	{
		if (side != CombatSide.Player)
		{
			return;
		}
		DamageReceivedThisTurn = 0m;
		return;
	}
}