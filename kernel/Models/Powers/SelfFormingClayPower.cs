using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core.Models.Powers;

public sealed class SelfFormingClayPower : PowerModel
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;


	public override void AfterBlockCleared(Creature creature)
	{
		if (creature == base.Owner)
		{
			CreatureCmd.GainBlock(base.Owner, base.Amount, ValueProp.Unpowered, null);
			PowerCmd.Remove(this);
		}
	}
}
