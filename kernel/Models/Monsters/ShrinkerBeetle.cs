using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
namespace MegaCrit.Sts2.Core.Models.Monsters;

public sealed class ShrinkerBeetle : MonsterModel
{
	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 40, 38);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 42, 40);

	private int ChompDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 8, 7);

	private int StompDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 14, 13);

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("SHRINKER_MOVE", SyncMove(ShrinkMove), new DebuffIntent(strong: true));
		MoveState moveState2 = new MoveState("CHOMP_MOVE", SyncMove(ChompMove), new SingleAttackIntent(ChompDamage));
		MoveState moveState3 = new MoveState("STOMP_MOVE", SyncMove(StompMove), new SingleAttackIntent(StompDamage));
		moveState.FollowUpState = moveState2;
		moveState2.FollowUpState = moveState3;
		moveState3.FollowUpState = moveState2;
		list.Add(moveState);
		list.Add(moveState2);
		list.Add(moveState3);
		return new MonsterMoveStateMachine(list, moveState);
	}

	private void ShrinkMove(IReadOnlyList<Creature> targets)
	{
		PowerCmd.Apply<ShrinkPower>(targets, -1m, base.Creature, null);
	}

	private void ChompMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(ChompDamage).FromMonster(this)
			.Execute(null);
	}

	private void StompMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(StompDamage).FromMonster(this)
			.Execute(null);
	}
}

