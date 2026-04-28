using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core.Models.Powers.Mocks;

public sealed class MockGainBlockOnAttackPower : PowerModel
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Single;

	public override void AfterAttack(AttackCommand command)
	{
		if (command.Attacker == base.Owner && command.DamageProps.HasFlag(ValueProp.Move))
		{
			CreatureCmd.GainBlock(base.Owner, 1m, ValueProp.Unpowered, null);
		}
	}
}
