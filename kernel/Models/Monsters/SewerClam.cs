using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace MegaCrit.Sts2.Core.Models.Monsters;

public sealed class SewerClam : MonsterModel
{
	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 58, 56);

	public override int MaxInitialHp => MinInitialHp;

	private int JetDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 11, 10);

	public override void AfterAddedToRoom()
	{
		base.AfterAddedToRoom();
		int valueIfAscension = AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 9, 8);
		PowerCmd.Apply<PlatingPower>(base.Creature, valueIfAscension, base.Creature, null);
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("PRESSURIZE_MOVE", SyncMove(PressurizeMove), new BuffIntent());
		MoveState moveState2 = (MoveState)(moveState.FollowUpState = new MoveState("JET_MOVE", SyncMove(JetMove), new SingleAttackIntent(JetDamage)));
		moveState2.FollowUpState = moveState;
		list.Add(moveState);
		list.Add(moveState2);
		return new MonsterMoveStateMachine(list, moveState2);
	}

	private void PressurizeMove(IReadOnlyList<Creature> targets)
	{
		PowerCmd.Apply<StrengthPower>(base.Creature, 4m, base.Creature, null);
	}

	private void JetMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(JetDamage).FromMonster(this)
			.Execute(null);
	}

	
}

