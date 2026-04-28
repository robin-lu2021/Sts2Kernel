using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Hooks;
namespace MegaCrit.Sts2.Core.Models.Powers;

public sealed class DoomPower : PowerModel
{
	public override PowerType Type => PowerType.Debuff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public static void DoomKill(IReadOnlyList<Creature> creatures)
	{
		if (creatures.Count == 0)
		{
			return;
		}
		CombatState combatState = creatures.First().CombatState;
		foreach (Creature creature in creatures)
		{
			CreatureCmd.Kill(creature);
		}
		Hook.AfterDiedToDoom(combatState, creatures);
	}

	public static IReadOnlyList<Creature> GetDoomedCreatures(IReadOnlyList<Creature> creatures)
	{
		return creatures.Where((Creature c) => c.GetPower<DoomPower>()?.IsOwnerDoomed() ?? false).ToList();
	}

	public bool IsOwnerDoomed()
	{
		return base.Owner.CurrentHp <= base.Amount;
	}

	public override void BeforeTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
	{
		if (!CombatManager.Instance.IsOverOrEnding && side == base.Owner.Side && !base.Owner.IsDead && IsOwnerDoomed())
		{
			IReadOnlyList<Creature> doomedCreatures = GetDoomedCreatures(base.Owner.CombatState.GetCreaturesOnSide(side));
			if (doomedCreatures.First() == base.Owner)
			{
				DoomKill(doomedCreatures);
			}
		}
	}
}
