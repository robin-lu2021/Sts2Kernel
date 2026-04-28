using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.TestSupport;

namespace MegaCrit.Sts2.Core.Models.Monsters;

public sealed class BygoneEffigy : MonsterModel
{
	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 132, 127);

	public override int MaxInitialHp => MinInitialHp;

	private int SlashDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 15, 13);

	public override void AfterAddedToRoom()
	{
		base.AfterAddedToRoom();
		PowerCmd.Apply<SlowPower>(base.Creature, 1m, base.Creature, null);
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("INITIAL_SLEEP_MOVE", InitialSleepMove, new SleepIntent());
		MoveState moveState2 = new MoveState("WAKE_MOVE", WakeMove, new BuffIntent());
		MoveState moveState3 = new MoveState("SLEEP_MOVE", SleepMove, new SleepIntent());
		MoveState moveState4 = new MoveState("SLASHES_MOVE", SlashMove, new SingleAttackIntent(SlashDamage));
		moveState.FollowUpState = moveState2;
		moveState2.FollowUpState = moveState4;
		moveState3.FollowUpState = moveState4;
		moveState4.FollowUpState = moveState4;
		list.Add(moveState);
		list.Add(moveState2);
		list.Add(moveState3);
		list.Add(moveState4);
		return new MonsterMoveStateMachine(list, moveState);
	}

	private void InitialSleepMove(IReadOnlyList<Creature> targets)
	{
		LocString line = MonsterModel.L10NMonsterLookup("BYGONE_EFFIGY.moves.SLEEP.speakLine1");
	}

	private void SleepMove(IReadOnlyList<Creature> targets)
	{
		return;
	}

	private void WakeMove(IReadOnlyList<Creature> targets)
	{
		PowerCmd.Apply<StrengthPower>(base.Creature, 10m, base.Creature, null);
		LocString line = MonsterModel.L10NMonsterLookup("BYGONE_EFFIGY.moves.SLEEP.speakLine2");
	}

	private void SlashMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(SlashDamage).FromMonster(this)
			.Execute(null);
	}

	
}

