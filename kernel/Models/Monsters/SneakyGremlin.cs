using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace MegaCrit.Sts2.Core.Models.Monsters;

public sealed class SneakyGremlin : MonsterModel
{
	private const string _wakeUpTrigger = "WakeUpTrigger";

	private bool _isAwake;

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 11, 10);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 15, 14);

	private int TackleDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 10, 9);

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
		MoveState moveState2 = (MoveState)(moveState.FollowUpState = new MoveState("TACKLE_MOVE", SyncMove(TackleMove), new SingleAttackIntent(TackleDamage)));
		moveState2.FollowUpState = moveState2;
		list.Add(moveState);
		list.Add(moveState2);
		return new MonsterMoveStateMachine(list, moveState);
	}

	private void SpawnedMove(IReadOnlyList<Creature> targets)
	{
		IsAwake = true;
	}

	private void TackleMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(TackleDamage).FromMonster(this)
			.Execute(null);
	}

	
}

