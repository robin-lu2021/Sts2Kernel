using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core.Models.Monsters;

public sealed class SlitheringStrangler : MonsterModel
{
	private const string _attackDefendTrigger = "AttackDefendTrigger";

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 54, 53);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 56, 55);

	private int ThwackDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 8, 7);

	private int LashDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 13, 12);

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("CONSTRICT", SyncMove(ConstrictMove), new DebuffIntent());
		MoveState moveState2 = new MoveState("TWACK", SyncMove(ThwackMove), new SingleAttackIntent(ThwackDamage), new DefendIntent());
		MoveState moveState3 = new MoveState("LASH", SyncMove(LashMove), new SingleAttackIntent(LashDamage));
		RandomBranchState randomBranchState = (RandomBranchState)(moveState.FollowUpState = new RandomBranchState("rand"));
		moveState2.FollowUpState = moveState;
		moveState3.FollowUpState = moveState;
		randomBranchState.AddBranch(moveState2, MoveRepeatType.CanRepeatForever);
		randomBranchState.AddBranch(moveState3, MoveRepeatType.CanRepeatForever);
		list.Add(randomBranchState);
		list.Add(moveState2);
		list.Add(moveState);
		list.Add(moveState3);
		return new MonsterMoveStateMachine(list, moveState);
	}

	private void ConstrictMove(IReadOnlyList<Creature> targets)
	{
		PowerCmd.Apply<ConstrictPower>(targets, 3m, base.Creature, null);
	}

	private void ThwackMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(ThwackDamage).FromMonster(this)
			.Execute(null);
		CreatureCmd.GainBlock(base.Creature, 5m, ValueProp.Move, null);
	}

	private void LashMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(LashDamage).FromMonster(this)
			.Execute(null);
	}

	
}

