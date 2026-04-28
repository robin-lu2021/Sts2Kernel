using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
namespace MegaCrit.Sts2.Core.Models.Monsters;

public sealed class SlumberingBeetle : MonsterModel
{
	public const string wakeUpTrigger = "WakeUp";

	private const string _rolloutTrigger = "Rollout";

	public const string rolloutMoveId = "ROLL_OUT_MOVE";
	
	private bool _isAwake;

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 89, 86);

	public override int MaxInitialHp => MinInitialHp;

	private int RolloutDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 18, 16);

	private int PlatingAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 18, 15);

	public bool IsAwake
	{
		get
		{
			return _isAwake;
		}
		set
		{
			AssertMutable();
			_isAwake = value;
		}
	}

	public override void AfterAddedToRoom()
	{
		base.AfterAddedToRoom();
		PowerCmd.Apply<PlatingPower>(base.Creature, PlatingAmount, base.Creature, null);
		PowerCmd.Apply<SlumberPower>(base.Creature, 3m, base.Creature, null);
	}

	public void WakeUpMove(IReadOnlyList<Creature> _)
	{
		IsAwake = true;
		if (base.Creature.HasPower<PlatingPower>())
		{
			PowerCmd.Remove(base.Creature.GetPower<PlatingPower>());
		}
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("SNORE_MOVE", SyncMove(SnoreMove), new SleepIntent());
		MoveState moveState2 = new MoveState("ROLL_OUT_MOVE", SyncMove(RolloutMove), new SingleAttackIntent(RolloutDamage), new BuffIntent());
		ConditionalBranchState conditionalBranchState = (ConditionalBranchState)(moveState.FollowUpState = new ConditionalBranchState("SNORE_NEXT"));
		conditionalBranchState.AddState(moveState, () => base.Creature.HasPower<SlumberPower>());
		conditionalBranchState.AddState(moveState2, () => !base.Creature.HasPower<SlumberPower>());
		moveState2.FollowUpState = moveState2;
		list.Add(moveState);
		list.Add(conditionalBranchState);
		list.Add(moveState2);
		return new MonsterMoveStateMachine(list, moveState);
	}

	private void SnoreMove(IReadOnlyList<Creature> targets)
	{
		return;
	}

	private void RolloutMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(RolloutDamage).FromMonster(this)
			.Execute(null);
		PowerCmd.Apply<StrengthPower>(base.Creature, 2m, base.Creature, null);
	}

	
}

