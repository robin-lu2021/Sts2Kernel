using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Random;

namespace MegaCrit.Sts2.Core.Models.Monsters;

public sealed class ScrollOfBiting : MonsterModel
{
	private int _starterMoveIdx;

	private const string _attackDoubleTrigger = "ATTACK_DOUBLE";

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 32, 31);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 39, 38);

	private int ChompDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 16, 14);

	private int ChewDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 6, 5);

	public int StarterMoveIdx
	{
		get
		{
			return _starterMoveIdx;
		}
		set
		{
			AssertMutable();
			_starterMoveIdx = value;
		}
	}

	public override void AfterAddedToRoom()
	{
		base.AfterAddedToRoom();
		PowerCmd.Apply<PaperCutsPower>(base.Creature, 2m, base.Creature, null);
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("CHOMP", SyncMove(ChompMove), new SingleAttackIntent(ChompDamage));
		MoveState moveState2 = new MoveState("CHEW", SyncMove(ChewState), new MultiAttackIntent(ChewDamage, 2));
		MoveState moveState3 = new MoveState("MORE_TEETH", SyncMove(MoreTeethMove), new BuffIntent());
		RandomBranchState randomBranchState = new RandomBranchState("rand");
		moveState.FollowUpState = moveState3;
		moveState2.FollowUpState = randomBranchState;
		moveState3.FollowUpState = moveState2;
		randomBranchState.AddBranch(moveState, MoveRepeatType.CannotRepeat);
		randomBranchState.AddBranch(moveState2, 2);
		list.Add(moveState);
		list.Add(moveState2);
		list.Add(moveState3);
		list.Add(randomBranchState);
		return (StarterMoveIdx % 3) switch
		{
			0 => new MonsterMoveStateMachine(list, moveState), 
			1 => new MonsterMoveStateMachine(list, moveState2), 
			_ => new MonsterMoveStateMachine(list, moveState3), 
		};
	}

	private void ChompMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(ChompDamage).FromMonster(this)
			.Execute(null);
	}

	private void ChewState(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(ChewDamage).WithHitCount(2).FromMonster(this)
			.Execute(null);
	}

	private void MoreTeethMove(IReadOnlyList<Creature> targets)
	{
		PowerCmd.Apply<StrengthPower>(base.Creature, 2m, base.Creature, null);
	}

	
}

