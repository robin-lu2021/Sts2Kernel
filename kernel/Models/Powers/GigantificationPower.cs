using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core.Models.Powers;

public sealed class GigantificationPower : PowerModel
{
	private class Data
	{
		public AttackCommand? commandToModify;
	}

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	protected override object InitInternalData()
	{
		return new Data();
	}

	public override void BeforeAttack(AttackCommand command)
	{
		if (!(command.ModelSource is CardModel cardModel))
		{
			return;
		}
		if (cardModel.Owner.Creature != base.Owner)
		{
			return;
		}
		if (cardModel.Type != CardType.Attack)
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
		internalData.commandToModify = command;
		return;
	}

	public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		if (cardSource == null)
		{
			return 1m;
		}
		if (cardSource.Owner.Creature != base.Owner)
		{
			return 1m;
		}
		if (!props.IsPoweredAttack())
		{
			return 1m;
		}
		Data internalData = GetInternalData<Data>();
		if (internalData.commandToModify == null || cardSource == internalData.commandToModify.ModelSource)
		{
			return 3m;
		}
		return 1m;
	}

	public override void AfterAttack(AttackCommand command)
	{
		Data internalData = GetInternalData<Data>();
		if (command == internalData.commandToModify)
		{
			internalData.commandToModify = null;
			PowerCmd.Decrement(this);
		}
	}
}
