using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace MegaCrit.Sts2.Core.Models.Monsters.Mocks;

public sealed class MockReattachMonster : MonsterModel
{
	private MoveState _deadState;

	public override LocString Title => MonsterModel.L10NMonsterLookup("BIG_DUMMY.name");

	public override int MinInitialHp => 1;

	public override int MaxInitialHp => 1;

	public MoveState DeadState
	{
		get
		{
			return _deadState;
		}
		private set
		{
			AssertMutable();
			_deadState = value;
		}
	}

	public override void AfterAddedToRoom()
	{
		base.AfterAddedToRoom();
		PowerCmd.Apply<ReattachPower>(base.Creature, 1m, base.Creature, null);
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("NOTHING", SyncMove(NothingMove), new HiddenIntent());
		moveState.FollowUpState = moveState;
		MoveState moveState2 = new MoveState("REATTACH_MOVE", SyncMove(ReattachMove), new HealIntent())
		{
			MustPerformOnceBeforeTransitioning = true,
			FollowUpState = moveState
		};
		DeadState = new MoveState("DEAD_MOVE", NothingMove)
		{
			FollowUpState = moveState2
		};
		list.Add(moveState);
		list.Add(moveState2);
		list.Add(DeadState);
		return new MonsterMoveStateMachine(list, moveState);
	}

	private void NothingMove(IReadOnlyList<Creature> targets)
	{
		return;
	}

	private void ReattachMove(IReadOnlyList<Creature> targets)
	{
		base.Creature.GetPower<ReattachPower>().DoReattach();
	}
}

