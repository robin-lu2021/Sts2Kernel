using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.TestSupport;

namespace MegaCrit.Sts2.Core.Models.Powers;

public sealed class SandpitPower : PowerModel
{
	private const float _paddingDistanceFromMonster = 450f;

	private const float _paddingDistanceFromOriginal = 50f;

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override bool IsInstanced => true;

	private IReadOnlyList<Creature> AllAffectedCreatures
	{
		get
		{
			Creature creature = base.Target.Player.Creature;
			IReadOnlyList<Creature> pets = base.Target.Pets;
			int num = 0;
			Creature[] array = new Creature[1 + pets.Count];
			array[num] = creature;
			num++;
			foreach (Creature item in pets)
			{
				array[num] = item;
				num++;
			}
			return new global::_003C_003Ez__ReadOnlyArray<Creature>(array);
		}
	}

	public override void AfterSideTurnStartLate(CombatSide side, CombatState combatState)
	{
		if (side == CombatSide.Enemy)
		{
			PowerCmd.Decrement(this);
		}
	}

	public override void AfterRemoved(Creature oldOwner)
	{
		if (oldOwner.IsDead || base.Target.IsDead)
		{
			return;
		}
		foreach (Creature allAffectedCreature2 in AllAffectedCreatures)
		{
			if (allAffectedCreature2.IsPlayer || allAffectedCreature2.Monster is Osty)
			{
				CreatureCmd.Kill(allAffectedCreature2, force: true);
			}
		}
	}
}
