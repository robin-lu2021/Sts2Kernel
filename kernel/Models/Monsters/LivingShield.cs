using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace MegaCrit.Sts2.Core.Models.Monsters;

public sealed class LivingShield : MonsterModel
{
	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 65, 55);

	public override int MaxInitialHp => MinInitialHp;

	private int ShieldSlamDamage => 6;

	private int SmashDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 18, 16);

	private int EnrageStr => 3;

	public override void AfterAddedToRoom()
	{
		base.AfterAddedToRoom();
		PowerCmd.Apply<RampartPower>(base.Creature, 25m, base.Creature, null);
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("SHIELD_SLAM_MOVE", SyncMove(ShieldSlamMove), new SingleAttackIntent(ShieldSlamDamage));
		ConditionalBranchState conditionalBranchState = new ConditionalBranchState("SHIELD_SLAM_BRANCH");
		MoveState moveState2 = new MoveState("SMASH_MOVE", SyncMove(SmashMove), new SingleAttackIntent(SmashDamage), new BuffIntent());
		moveState.FollowUpState = conditionalBranchState;
		conditionalBranchState.AddState(moveState, () => GetAllyCount() > 0);
		conditionalBranchState.AddState(moveState2, () => GetAllyCount() == 0);
		moveState2.FollowUpState = moveState2;
		list.Add(moveState);
		list.Add(moveState2);
		list.Add(conditionalBranchState);
		return new MonsterMoveStateMachine(list, moveState);
	}

	private void ShieldSlamMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(ShieldSlamDamage).FromMonster(this)
			.Execute(null);
	}

	private void SmashMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(SmashDamage).FromMonster(this)
			.Execute(null);
		PowerCmd.Apply<StrengthPower>(base.Creature, EnrageStr, base.Creature, null);
	}

	private int GetAllyCount()
	{
		return base.Creature.CombatState.GetTeammatesOf(base.Creature).Count((Creature c) => c.IsAlive && c != base.Creature);
	}
}

