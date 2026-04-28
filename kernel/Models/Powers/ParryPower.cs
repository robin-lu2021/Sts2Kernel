using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core.Models.Powers;

public class ParryPower : PowerModel
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public void AfterSovereignBladePlayed(Creature? dealer, IEnumerable<DamageResult> damageResults)
	{
		if (dealer != null && dealer == base.Owner)
		{
			 
			CreatureCmd.GainBlock(dealer, base.Amount, ValueProp.Unpowered, null);
		}
	}
}
