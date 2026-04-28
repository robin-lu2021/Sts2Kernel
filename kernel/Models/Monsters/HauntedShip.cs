using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace MegaCrit.Sts2.Core.Models.Monsters;

public sealed class HauntedShip : MonsterModel
{
	private const string _attackTripleTrigger = "AttackTriple";

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 67, 63);

	public override int MaxInitialHp => MinInitialHp;

	private int HauntDazed => 5;

	private int RammingSpeedDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 11, 10);

	private int SwipeDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 14, 13);

	private int StompDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 5, 4);

	private int StompRepeat => 3;

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("RAMMING_SPEED_MOVE", RammingSpeedMove, new SingleAttackIntent(RammingSpeedDamage), new DebuffIntent());
		MoveState moveState2 = new MoveState("SWIPE_MOVE", SwipeMove, new SingleAttackIntent(SwipeDamage));
		MoveState moveState3 = new MoveState("STOMP_MOVE", StompMove, new MultiAttackIntent(StompDamage, StompRepeat));
		MoveState moveState4 = new MoveState("HAUNT_MOVE", HauntMove, new StatusIntent(HauntDazed));
		RandomBranchState randomBranchState = (RandomBranchState)(moveState4.FollowUpState = (moveState3.FollowUpState = (moveState2.FollowUpState = (moveState.FollowUpState = new RandomBranchState("RAND")))));
		randomBranchState.AddBranch(moveState, MoveRepeatType.CannotRepeat, () => (base.CombatState.RoundNumber % 2 != 0) ? 1 : 0);
		randomBranchState.AddBranch(moveState2, MoveRepeatType.CannotRepeat, () => (base.CombatState.RoundNumber % 2 != 0) ? 1 : 0);
		randomBranchState.AddBranch(moveState3, MoveRepeatType.CannotRepeat, () => (base.CombatState.RoundNumber % 2 != 0) ? 1 : 0);
		list.Add(randomBranchState);
		list.Add(moveState);
		list.Add(moveState2);
		list.Add(moveState3);
		list.Add(moveState4);
		return new MonsterMoveStateMachine(list, moveState4);
	}

	private void RammingSpeedMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(RammingSpeedDamage).FromMonster(this)
			.Execute(null);
		PowerCmd.Apply<WeakPower>(targets, 1m, base.Creature, null);
	}

	private void SwipeMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(SwipeDamage).FromMonster(this)
			.Execute(null);
	}

	private void StompMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(StompDamage).WithHitCount(StompRepeat).FromMonster(this)
			.Execute(null);
	}

	private void HauntMove(IReadOnlyList<Creature> targets)
	{
		CardPileCmd.AddToCombatAndPreview<Dazed>(targets, PileType.Discard, HauntDazed, addedByPlayer: false);
	}

	
}

