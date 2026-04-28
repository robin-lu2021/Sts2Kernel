using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace MegaCrit.Sts2.Core.Models.Monsters;

public sealed class Byrdonis : MonsterModel
{
	private const string _angryTrigger = "Angry";

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 90, 81);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 90, 84);

	private static int PeckDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 4, 3);

	private static int PeckRepeat => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 3, 3);

	private static int SwoopDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 19, 17);

	public override void AfterAddedToRoom()
	{
		base.AfterAddedToRoom();
		PowerCmd.Apply<TerritorialPower>(base.Creature, 1m, base.Creature, null);
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("PECK_MOVE", PeckMove, new MultiAttackIntent(PeckDamage, PeckRepeat));
		MoveState moveState2 = new MoveState("SWOOP_MOVE", SwoopMove, new SingleAttackIntent(SwoopDamage));
		moveState.FollowUpState = moveState2;
		moveState2.FollowUpState = moveState;
		list.Add(moveState2);
		list.Add(moveState);
		return new MonsterMoveStateMachine(list, moveState2);
	}

	private void PeckMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(PeckDamage).WithHitCount(PeckRepeat).FromMonster(this)
			.Execute(null);
	}

	private void SwoopMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(SwoopDamage).FromMonster(this)
			.Execute(null);
	}

	
}

