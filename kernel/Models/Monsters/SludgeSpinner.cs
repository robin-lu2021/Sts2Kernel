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

public sealed class SludgeSpinner : MonsterModel
{
	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 41, 37);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 42, 39);

	private int OilSprayDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 9, 8);

	private int SlamDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 12, 11);

	private int RageDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 7, 6);

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("OIL_SPRAY_MOVE", SyncMove(OilSprayMove), new SingleAttackIntent(OilSprayDamage), new DebuffIntent());
		MoveState moveState2 = new MoveState("SLAM_MOVE", SyncMove(SlamMove), new SingleAttackIntent(SlamDamage));
		MoveState moveState3 = new MoveState("RAGE_MOVE", SyncMove(RageMove), new SingleAttackIntent(RageDamage), new BuffIntent());
		RandomBranchState randomBranchState = (RandomBranchState)(moveState3.FollowUpState = (moveState2.FollowUpState = (moveState.FollowUpState = new RandomBranchState("RAND"))));
		randomBranchState.AddBranch(moveState, MoveRepeatType.CannotRepeat);
		randomBranchState.AddBranch(moveState2, MoveRepeatType.CannotRepeat);
		randomBranchState.AddBranch(moveState3, MoveRepeatType.CannotRepeat);
		list.Add(randomBranchState);
		list.Add(moveState);
		list.Add(moveState2);
		list.Add(moveState3);
		return new MonsterMoveStateMachine(list, moveState);
	}

	private void OilSprayMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(OilSprayDamage).FromMonster(this)
			.Execute(null);
		PowerCmd.Apply<WeakPower>(targets, 1m, base.Creature, null);
	}

	private void SlamMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(SlamDamage).FromMonster(this)
			.Execute(null);
	}

	private void RageMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(RageDamage).FromMonster(this)
			.Execute(null);
		PowerCmd.Apply<StrengthPower>(base.Creature, 3m, base.Creature, null);
	}

	
}

