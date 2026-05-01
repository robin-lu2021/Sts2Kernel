using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Debug;
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
		PhrogDebug.LogInfo($"InfestedPower.AfterDeath: ownerCombatId={base.Owner.CombatId?.ToString() ?? "null"}, targetCombatId={target.CombatId?.ToString() ?? "null"}, ownerIsTarget={base.Owner == target}, prevented={wasRemovalPrevented}, localNetId={LocalContext.NetId?.ToString() ?? "null"}, combatInProgress={CombatManager.Instance.IsInProgress}, combatEnding={CombatManager.Instance.IsEnding}, combatStateNull={base.CombatState == null}");
		if (!wasRemovalPrevented && base.Owner == target)
		{
			if (TestMode.IsOff)
			{
			}
			for (int i = 0; i < 4; i++)
			{
				Wriggler wriggler = (Wriggler)KernelModelDb.Monster<Wriggler>().ToMutable();
				wriggler.StartStunned = true;
				Creature creature = CreatureCmd.Add(wriggler, base.CombatState, base.Owner.Side, PhrogParasiteElite.GetWrigglerSlotName(i));
				PhrogDebug.LogInfo($"InfestedPower spawned wriggler: index={i}, combatId={creature.CombatId?.ToString() ?? "null"}, slot={creature.SlotName ?? "null"}, hp={creature.CurrentHp}/{creature.MaxHp}");
			}
			PhrogDebug.LogInfo($"InfestedPower.AfterDeath complete: enemies={base.CombatState.Enemies.Count}, aliveEnemies={base.CombatState.Enemies.Count(e => e.IsAlive)}");
		}
	}

	public override bool ShouldStopCombatFromEnding()
	{
		return true;
	}
}
