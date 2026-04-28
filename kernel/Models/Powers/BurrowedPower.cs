using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace MegaCrit.Sts2.Core.Models.Powers;

public sealed class BurrowedPower : PowerModel
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Single;

	public override bool ShouldClearBlock(Creature creature)
	{
		if (base.Owner != creature)
		{
			return true;
		}
		return false;
	}

	public override void AfterBlockBroken(Creature creature)
	{
		if (creature == base.Owner)
		{
			CreatureCmd.Stun(base.Owner, "BITE_MOVE");
			PowerCmd.Remove<BurrowedPower>(base.Owner);
		}
	}

	public override void AfterRemoved(Creature oldOwner)
	{
		CreatureCmd.LoseBlock(oldOwner, 999999999m);
	}
}
