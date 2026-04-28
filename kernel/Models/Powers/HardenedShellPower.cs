using System;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core.Models.Powers;

public sealed class HardenedShellPower : PowerModel
{
	private class Data
	{
		public decimal damageReceivedThisTurn;
	}

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override bool ShouldScaleInMultiplayer => true;

	public override int DisplayAmount => (int)Math.Max(0m, (decimal)base.Amount - GetInternalData<Data>().damageReceivedThisTurn);

	protected override object InitInternalData()
	{
		return new Data();
	}

	public override decimal ModifyHpLostBeforeOstyLate(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		if (target != base.Owner)
		{
			return amount;
		}
		if (amount == 0m)
		{
			return amount;
		}
		return Math.Min(amount, (decimal)base.Amount - GetInternalData<Data>().damageReceivedThisTurn);
	}

	public override void AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		if (target != base.Owner)
		{
			return;
		}
		if (result.WasFullyBlocked)
		{
			return;
		}
		GetInternalData<Data>().damageReceivedThisTurn += (decimal)result.UnblockedDamage;
		InvokeDisplayAmountChanged();
		return;
	}

	public override void BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, CombatState combatState)
	{
		if (side != CombatSide.Player)
		{
			return;
		}
		GetInternalData<Data>().damageReceivedThisTurn = default(decimal);
		InvokeDisplayAmountChanged();
		return;
	}
}
