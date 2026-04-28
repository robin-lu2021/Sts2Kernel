using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Monsters;

namespace MegaCrit.Sts2.Core.Models.Powers;

public sealed class SurprisePower : PowerModel
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Single;

	public override void AfterDeath(PlayerChoiceContext choiceContext, Creature target, bool wasRemovalPrevented, float deathAnimLength)
	{
		if (wasRemovalPrevented || base.Owner != target)
		{
			return;
		}
		CreatureCmd.Add<SneakyGremlin>(base.CombatState, "sneaky");
		Creature fatGremlin = CreatureCmd.Add<FatGremlin>(base.CombatState, "fat");
		foreach (ThieveryPower powerInstance in base.Owner.GetPowerInstances<ThieveryPower>())
		{
			HeistPower heistPower = (HeistPower)KernelModelDb.Power<HeistPower>().ToMutable();
			heistPower.Target = powerInstance.Target;
			PowerCmd.Apply(heistPower, fatGremlin, powerInstance.DynamicVars.Gold.IntValue, base.Owner, null);
		}
	}

	public override bool ShouldStopCombatFromEnding()
	{
		return true;
	}
}
