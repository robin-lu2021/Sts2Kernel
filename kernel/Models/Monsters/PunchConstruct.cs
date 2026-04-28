using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core.Models.Monsters;

public sealed class PunchConstruct : MonsterModel
{
	private const string _attackDoubleTrigger = "DoubleAttack";

	private bool _startsWithStrongPunch;

	private int _startingHpReduction;

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 60, 55);

	public override int MaxInitialHp => MinInitialHp;

	private int StrongPunchDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 16, 14);

	private int FastPunchDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 6, 5);

	private int FastPunchRepeat => 2;

	public bool StartsWithStrongPunch
	{
		get
		{
			return _startsWithStrongPunch;
		}
		set
		{
			AssertMutable();
			_startsWithStrongPunch = value;
		}
	}

	public int StartingHpReduction
	{
		get
		{
			return _startingHpReduction;
		}
		set
		{
			AssertMutable();
			_startingHpReduction = value;
		}
	}

	public override void AfterAddedToRoom()
	{
		base.AfterAddedToRoom();
		PowerCmd.Apply<ArtifactPower>(base.Creature, 1m, base.Creature, null);
		if (StartingHpReduction > 0)
		{
			base.Creature.SetCurrentHpInternal(Math.Max(1, base.Creature.CurrentHp - StartingHpReduction));
		}
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("READY_MOVE", SyncMove(ReadyMove), new DefendIntent());
		MoveState moveState2 = new MoveState("STRONG_PUNCH_MOVE", SyncMove(StrongPunchMove), new SingleAttackIntent(StrongPunchDamage));
		MoveState moveState3 = new MoveState("FAST_PUNCH_MOVE", SyncMove(FastPunchMove), new MultiAttackIntent(FastPunchDamage, FastPunchRepeat), new DebuffIntent());
		moveState.FollowUpState = moveState2;
		moveState2.FollowUpState = moveState3;
		moveState3.FollowUpState = moveState;
		list.Add(moveState);
		list.Add(moveState3);
		list.Add(moveState2);
		return new MonsterMoveStateMachine(list, StartsWithStrongPunch ? moveState2 : moveState);
	}

	private void ReadyMove(IReadOnlyList<Creature> targets)
	{
		CreatureCmd.GainBlock(base.Creature, 10m, ValueProp.Move, null);
	}

	private void StrongPunchMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(StrongPunchDamage).FromMonster(this)
			.Execute(null);
	}

	private void FastPunchMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(FastPunchDamage).WithHitCount(FastPunchRepeat).FromMonster(this)
			.OnlyPlayAnimOnce()
			.Execute(null);
		PowerCmd.Apply<WeakPower>(targets, 1m, base.Creature, null);
	}

	
}

