using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace MegaCrit.Sts2.Core.Models.Monsters;

public sealed class FatGremlin : MonsterModel
{
	private const string _fleeTrigger = "FleeTrigger";

	private const string _wakeUpTrigger = "WakeUpTrigger";

	private bool _isAwake;

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 14, 13);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 18, 17);

	private bool IsAwake
	{
		get
		{
			return _isAwake;
		}
		set
		{
			AssertMutable();
			_isAwake = value;
		}
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("SPAWNED_MOVE", SyncMove(SpawnedMove), new StunIntent());
		MoveState moveState2 = (MoveState)(moveState.FollowUpState = new MoveState("FLEE_MOVE", SyncMove(FleeMove), new EscapeIntent()));
		moveState2.FollowUpState = moveState2;
		list.Add(moveState);
		list.Add(moveState2);
		return new MonsterMoveStateMachine(list, moveState);
	}

	private void SpawnedMove(IReadOnlyList<Creature> targets)
	{
		IsAwake = true;
	}

	private void FleeMove(IReadOnlyList<Creature> targets)
	{
		LocString line = MonsterModel.L10NMonsterLookup("FAT_GREMLIN.moves.FLEE.banter");
		CreatureCmd.Escape(base.Creature);
	}

	
}

