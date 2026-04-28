using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.TestSupport;

namespace MegaCrit.Sts2.Core.Models.Monsters;

public sealed class BowlbugSilk : MonsterModel
{
	private const int _thrashRepeat = 2;

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 41, 40);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 44, 43);

	private int ThrashDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 5, 4);

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("TRASH_MOVE", ThrashMove, new MultiAttackIntent(ThrashDamage, 2));
		MoveState moveState2 = (MoveState)(moveState.FollowUpState = new MoveState("TOXIC_SPIT_MOVE", SyncMove(WebMove), new DebuffIntent()));
		moveState2.FollowUpState = moveState;
		list.Add(moveState);
		list.Add(moveState2);
		return new MonsterMoveStateMachine(list, moveState2);
	}

	private void ThrashMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(ThrashDamage).WithHitCount(2).FromMonster(this)
			.Execute(null);
	}

	private void WebMove(IReadOnlyList<Creature> targets)
	{
		PowerCmd.Apply<WeakPower>(targets, 1m, base.Creature, null);
	}

	
}

