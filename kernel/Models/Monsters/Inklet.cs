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

public sealed class Inklet : MonsterModel
{
	private const string _attackTripleTrigger = "TRIPLE_ATTACK";

	private const int _whirlwindRepeat = 3;

	private bool _middleInklet;

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 12, 11);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 18, 17);

	private int JabDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 4, 3);

	private int WhirlwindDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 3, 2);

	private int PiercingGazeDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 11, 10);

	public bool MiddleInklet
	{
		get
		{
			return _middleInklet;
		}
		set
		{
			AssertMutable();
			_middleInklet = value;
		}
	}

	public override void AfterAddedToRoom()
	{
		base.AfterAddedToRoom();
		PowerCmd.Apply<SlipperyPower>(base.Creature, 1m, base.Creature, null);
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("JAB_MOVE", SyncMove(JabMove), new SingleAttackIntent(JabDamage));
		MoveState moveState2 = new MoveState("WHIRLWIND_MOVE", SyncMove(WhirlwindMove), new MultiAttackIntent(WhirlwindDamage, 3));
		MoveState moveState3 = new MoveState("PIERCING_GAZE_MOVE", SyncMove(PiercingGazeMove), new SingleAttackIntent(PiercingGazeDamage));
		RandomBranchState randomBranchState = new RandomBranchState("INIT_RAND");
		RandomBranchState randomBranchState2 = (RandomBranchState)(moveState2.FollowUpState = (moveState3.FollowUpState = (moveState.FollowUpState = new RandomBranchState("RAND"))));
		randomBranchState.AddBranch(moveState, 2, 1f);
		randomBranchState.AddBranch(moveState2, MoveRepeatType.CannotRepeat, 1f);
		randomBranchState2.AddBranch(moveState3, MoveRepeatType.CannotRepeat, 1f);
		randomBranchState2.AddBranch(moveState2, MoveRepeatType.CannotRepeat, 1f);
		moveState.FollowUpState = randomBranchState2;
		moveState2.FollowUpState = moveState;
		moveState3.FollowUpState = moveState;
		list.Add(moveState);
		list.Add(moveState3);
		list.Add(moveState2);
		list.Add(randomBranchState2);
		MoveState initialState = (_middleInklet ? moveState2 : moveState);
		return new MonsterMoveStateMachine(list, initialState);
	}

	private void JabMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(JabDamage).FromMonster(this)
			.Execute(null);
	}

	private void WhirlwindMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(WhirlwindDamage).WithHitCount(3).FromMonster(this)
			.Execute(null);
	}

	private void PiercingGazeMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(PiercingGazeDamage).FromMonster(this)
			.Execute(null);
	}

	
}

