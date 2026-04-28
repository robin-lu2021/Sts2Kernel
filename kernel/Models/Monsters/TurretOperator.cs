using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace MegaCrit.Sts2.Core.Models.Monsters;

public sealed class TurretOperator : MonsterModel
{
	private const int _fireRepeat = 5;

	private const string _crankTrigger = "Crank";

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 51, 41);

	public override int MaxInitialHp => MinInitialHp;

	private int FireDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 4, 3);

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("UNLOAD_MOVE_1", SyncMove(UnloadMove), new MultiAttackIntent(FireDamage, 5));
		MoveState moveState2 = new MoveState("UNLOAD_MOVE_2", SyncMove(UnloadMove), new MultiAttackIntent(FireDamage, 5));
		MoveState moveState3 = new MoveState("RELOAD_MOVE", SyncMove(ReloadMove), new BuffIntent());
		moveState.FollowUpState = moveState2;
		moveState2.FollowUpState = moveState3;
		moveState3.FollowUpState = moveState;
		list.Add(moveState);
		list.Add(moveState2);
		list.Add(moveState3);
		return new MonsterMoveStateMachine(list, moveState);
	}

	private void ReloadMove(IReadOnlyList<Creature> targets)
	{
		PowerCmd.Apply<StrengthPower>(base.Creature, 1m, base.Creature, null);
	}

	private void UnloadMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(FireDamage).WithHitCount(5).FromMonster(this)
			.Execute(null);
	}

	
}

