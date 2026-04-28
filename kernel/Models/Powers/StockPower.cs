using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Monsters;

namespace MegaCrit.Sts2.Core.Models.Powers;

public sealed class StockPower : PowerModel
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override void AfterDeath(PlayerChoiceContext choiceContext, Creature target, bool wasRemovalPrevented, float deathAnimLength)
	{
		if (!wasRemovalPrevented && target == base.Owner && base.Amount > 0)
		{
			Axebot axebot = (Axebot)KernelModelDb.Monster<Axebot>().ToMutable();
			axebot.ShouldPlaySpawnAnimation = true;
			axebot.StockAmount = base.Amount - 1;
		}
	}

	public override bool ShouldStopCombatFromEnding()
	{
		return true;
	}
}
