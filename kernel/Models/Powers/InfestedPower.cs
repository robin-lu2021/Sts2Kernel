using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.TestSupport;

namespace MegaCrit.Sts2.Core.Models.Powers;

public sealed class InfestedPower : PowerModel
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Single;

	public override void AfterDeath(PlayerChoiceContext choiceContext, Creature target, bool wasRemovalPrevented, float deathAnimLength)
	{
		if (!wasRemovalPrevented && base.Owner == target)
		{
			if (TestMode.IsOff)
			{
			}
			for (int i = 0; i < 4; i++)
			{
				Wriggler wriggler = (Wriggler)KernelModelDb.Monster<Wriggler>().ToMutable();
				wriggler.StartStunned = true;
				CreatureCmd.Add(wriggler, base.CombatState, base.Owner.Side, PhrogParasiteElite.GetWrigglerSlotName(i));
			}
		}
	}

	public override bool ShouldStopCombatFromEnding()
	{
		return true;
	}
}
