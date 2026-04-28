using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Monsters;

namespace MegaCrit.Sts2.Core.Models.Powers;

public sealed class RavenousPower : PowerModel
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override void AfterDeath(PlayerChoiceContext choiceContext, Creature target, bool wasRemovalPrevented, float deathAnimLength)
	{
		if (!wasRemovalPrevented && target != base.Owner && target.Side == base.Owner.Side && !base.Owner.IsDead)
		{
			((CorpseSlug)base.Owner.Monster).IsRavenous = true;
			CreatureCmd.Stun(base.Owner, StunnedMove);
			PowerCmd.Apply<StrengthPower>(base.Owner, base.Amount, base.Owner, null);
		}
	}

	private void StunnedMove(IReadOnlyList<Creature> targets)
	{
		((CorpseSlug)base.Owner.Monster).IsRavenous = false;
	}
}
