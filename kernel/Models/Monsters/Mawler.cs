using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace MegaCrit.Sts2.Core.Models.Monsters;

public sealed class Mawler : MonsterModel
{
	private const int _clawRepeat = 2;

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 76, 72);

	public override int MaxInitialHp => MinInitialHp;

	private int RipAndTearDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 16, 14);

	private int ClawDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 5, 4);

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("RIP_AND_TEAR_MOVE", SyncMove(RipAndTearMove), new SingleAttackIntent(RipAndTearDamage));
		MoveState moveState2 = new MoveState("ROAR_MOVE", SyncMove(RoarMove), new DebuffIntent());
		MoveState moveState3 = new MoveState("CLAW_MOVE", SyncMove(ClawMove), new MultiAttackIntent(ClawDamage, 2));
		RandomBranchState randomBranchState = (RandomBranchState)(moveState3.FollowUpState = (moveState2.FollowUpState = (moveState.FollowUpState = new RandomBranchState("RAND"))));
		randomBranchState.AddBranch(moveState, MoveRepeatType.CannotRepeat, 1f);
		randomBranchState.AddBranch(moveState2, MoveRepeatType.UseOnlyOnce, 1f);
		randomBranchState.AddBranch(moveState3, MoveRepeatType.CannotRepeat, 1f);
		list.Add(moveState);
		list.Add(moveState2);
		list.Add(moveState3);
		list.Add(randomBranchState);
		return new MonsterMoveStateMachine(list, moveState3);
	}

	private void RipAndTearMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(RipAndTearDamage).FromMonster(this)
			.Execute(null);
	}

	private void RoarMove(IReadOnlyList<Creature> targets)
	{
		PowerCmd.Apply<VulnerablePower>(targets, 3m, base.Creature, null);
	}

	private void ClawMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(ClawDamage).WithHitCount(2).FromMonster(this)
			.Execute(null);
	}

	
}

