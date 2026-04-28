using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core.Models.Powers;

public sealed class VigorPower : PowerModel
{
	private class Data
	{
		public AttackCommand? commandToModify;

		public int amountWhenAttackStarted;
	}

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	protected override object InitInternalData()
	{
		return new Data();
	}

	public override void BeforeAttack(AttackCommand command)
	{
		if (command.Attacker != base.Owner)
		{
			return;
		}
		if (!command.DamageProps.IsPoweredAttack())
		{
			return;
		}
		Data internalData = GetInternalData<Data>();
		if (internalData.commandToModify != null)
		{
			return;
		}
		if (command.ModelSource != null && !(command.ModelSource is CardModel))
		{
			return;
		}
		if (!command.DamageProps.IsPoweredAttack())
		{
			return;
		}
		internalData.commandToModify = command;
		internalData.amountWhenAttackStarted = base.Amount;
		return;
	}

	public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		if (base.Owner != dealer)
		{
			return 0m;
		}
		if (!props.IsPoweredAttack())
		{
			return 0m;
		}
		Data internalData = GetInternalData<Data>();
		if (internalData.commandToModify != null && cardSource != null && cardSource != internalData.commandToModify.ModelSource)
		{
			return 0m;
		}
		if (internalData.commandToModify != null && internalData.commandToModify.Attacker != dealer)
		{
			return 0m;
		}
		return base.Amount;
	}

	public override void AfterAttack(AttackCommand command)
	{
		Data internalData = GetInternalData<Data>();
		if (command == internalData.commandToModify)
		{
			PowerCmd.ModifyAmount(this, -internalData.amountWhenAttackStarted, null, null);
		}
	}
}
