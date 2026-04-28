using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace MegaCrit.Sts2.Core.Models.Monsters;

public sealed class BowlbugNectar : MonsterModel
{
	private const string _buffTrigger = "Buff";

	private const string _spineSkin = "goop";

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 36, 35);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 39, 38);

	private int ThrashDamage => 3;

	private int BuffStrengthGain => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 16, 15);

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("THRASH_MOVE", ThrashMove, new SingleAttackIntent(ThrashDamage));
		MoveState moveState2 = new MoveState("BUFF_MOVE", BuffMove, new BuffIntent());
		MoveState moveState3 = new MoveState("THRASH2_MOVE", ThrashMove, new SingleAttackIntent(ThrashDamage));
		moveState.FollowUpState = moveState2;
		moveState2.FollowUpState = moveState3;
		moveState3.FollowUpState = moveState3;
		list.Add(moveState2);
		list.Add(moveState3);
		list.Add(moveState);
		return new MonsterMoveStateMachine(list, moveState);
	}

	private void ThrashMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(ThrashDamage).FromMonster(this)
			.Execute(null);
	}

	private void BuffMove(IReadOnlyList<Creature> targets)
	{
		PowerCmd.Apply<StrengthPower>(base.Creature, BuffStrengthGain, base.Creature, null);
	}
}

