using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace MegaCrit.Sts2.Core.Models.Monsters;

public sealed class TheAdversaryMkOne : MonsterModel
{
	public override int MinInitialHp => 100;

	public override int MaxInitialHp => MinInitialHp;

	private int SmashDamage => 12;

	private int BeamDamage => 15;

	private int BarrageDamage => 8;

	private int BarrageRepeat => 2;

	public override void AfterAddedToRoom()
	{
		base.AfterAddedToRoom();
		PowerCmd.Apply<ArtifactPower>(base.Creature, 0m, base.Creature, null);
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("SMASH_MOVE", SyncMove(SmashMove), new SingleAttackIntent(SmashDamage));
		MoveState moveState2 = new MoveState("BEAM_MOVE", SyncMove(BeamMove), new SingleAttackIntent(BeamDamage));
		MoveState moveState3 = new MoveState("BARRAGE_MOVE", SyncMove(BarrageMove), new MultiAttackIntent(BarrageDamage, BarrageRepeat), new BuffIntent());
		moveState.FollowUpState = moveState2;
		moveState2.FollowUpState = moveState3;
		moveState3.FollowUpState = moveState;
		list.Add(moveState);
		list.Add(moveState2);
		list.Add(moveState3);
		return new MonsterMoveStateMachine(list, moveState);
	}

	private void SmashMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(SmashDamage).FromMonster(this)
			.Execute(null);
	}

	private void BeamMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(BeamDamage).FromMonster(this)
			.Execute(null);
	}

	private void BarrageMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(BarrageDamage).WithHitCount(BarrageRepeat).FromMonster(this)
			.Execute(null);
		PowerCmd.Apply<StrengthPower>(base.Creature, 2m, base.Creature, null);
	}
}

