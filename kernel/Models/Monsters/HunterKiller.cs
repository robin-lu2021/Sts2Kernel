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

public sealed class HunterKiller : MonsterModel
{
	private const string _tripleAttackTrigger = "TripleAttack";

	private const int _punctureRepeat = 3;

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 126, 121);

	public override int MaxInitialHp => MinInitialHp;

	private int BiteDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 19, 17);

	private int PunctureDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 8, 7);

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("TENDERIZING_GOOP_MOVE", SyncMove(GoopMove), new DebuffIntent());
		MoveState moveState2 = new MoveState("BITE_MOVE", SyncMove(BiteMove), new SingleAttackIntent(BiteDamage));
		MoveState moveState3 = new MoveState("PUNCTURE_MOVE", SyncMove(PunctureMove), new MultiAttackIntent(PunctureDamage, 3));
		RandomBranchState randomBranchState = (RandomBranchState)(moveState3.FollowUpState = (moveState2.FollowUpState = (moveState.FollowUpState = new RandomBranchState("RAND"))));
		randomBranchState.AddBranch(moveState2, MoveRepeatType.CannotRepeat);
		randomBranchState.AddBranch(moveState3, 2);
		list.Add(moveState);
		list.Add(moveState2);
		list.Add(moveState3);
		list.Add(randomBranchState);
		return new MonsterMoveStateMachine(list, moveState);
	}

	private void GoopMove(IReadOnlyList<Creature> targets)
	{
		PowerCmd.Apply<TenderPower>(targets, 1m, base.Creature, null);
	}

	private void BiteMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(BiteDamage).FromMonster(this)
			.Execute(null);
	}

	private void PunctureMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(PunctureDamage).WithHitCount(3)
			.FromMonster(this)
			.Execute(null);
	}

	
}

