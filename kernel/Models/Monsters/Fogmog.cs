using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace MegaCrit.Sts2.Core.Models.Monsters;

public sealed class Fogmog : MonsterModel
{
	private const string _summonTrigger = "Summon";

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 78, 74);

	public override int MaxInitialHp => MinInitialHp;

	private int SwipeDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 9, 8);

	private int HeadbuttDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 16, 14);

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("ILLUSION_MOVE", SyncMove(IllusionMove), new SummonIntent());
		MoveState moveState2 = new MoveState("SWIPE_MOVE", SyncMove(SwipeMove), new SingleAttackIntent(SwipeDamage), new BuffIntent());
		MoveState moveState3 = new MoveState("SWIPE_RANDOM_MOVE", SyncMove(SwipeMove), new SingleAttackIntent(SwipeDamage), new BuffIntent());
		MoveState moveState4 = new MoveState("HEADBUTT_MOVE", SyncMove(HeadbuttMove), new SingleAttackIntent(HeadbuttDamage));
		RandomBranchState randomBranchState = new RandomBranchState("BRANCH");
		randomBranchState.AddBranch(moveState3, MoveRepeatType.CannotRepeat, () => 0.4f);
		randomBranchState.AddBranch(moveState4, MoveRepeatType.CannotRepeat, () => 0.6f);
		moveState.FollowUpState = moveState2;
		moveState2.FollowUpState = randomBranchState;
		moveState3.FollowUpState = moveState4;
		moveState4.FollowUpState = moveState2;
		list.Add(moveState);
		list.Add(moveState2);
		list.Add(moveState3);
		list.Add(randomBranchState);
		list.Add(moveState4);
		return new MonsterMoveStateMachine(list, moveState);
	}

	private void IllusionMove(IReadOnlyList<Creature> targets)
	{
		CreatureCmd.Add<EyeWithTeeth>(base.CombatState, "illusion");
	}

	private void SwipeMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(SwipeDamage).FromMonster(this)
			.Execute(null);
		PowerCmd.Apply<StrengthPower>(base.Creature, 1m, base.Creature, null);
	}

	private void HeadbuttMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(HeadbuttDamage).FromMonster(this)
			.Execute(null);
	}

	
}

