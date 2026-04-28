using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core.Models.Monsters;

public sealed class Crusher : MonsterModel
{
	public override bool ShouldFadeAfterDeath => false;

	public override bool ShouldDisappearFromDoom => false;

	public override float DeathAnimLengthOverride => 2.5f;

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 219, 209);

	public override int MaxInitialHp => MinInitialHp;

	private int ThrashDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 14, 12);

	private int EnlargingStrikeDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 4, 4);

	private int BugStingDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 7, 6);

	private int BugStingTimes => 2;

	private int AdaptStrengthGain => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 3, 2);

	private int GuardedStrikeDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 14, 12);

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("THRASH_MOVE", SyncMove(ThrashMove), new SingleAttackIntent(ThrashDamage));
		MoveState moveState2 = new MoveState("ENLARGING_STRIKE_MOVE", SyncMove(EnlargingStrikeMove), new SingleAttackIntent(EnlargingStrikeDamage));
		MoveState moveState3 = new MoveState("BUG_STING_MOVE", SyncMove(BugStingMove), new MultiAttackIntent(BugStingDamage, BugStingTimes), new DebuffIntent());
		MoveState moveState4 = new MoveState("ADAPT_MOVE", SyncMove(AdaptMove), new BuffIntent());
		MoveState moveState5 = new MoveState("GUARDED_STRIKE_MOVE", SyncMove(GuardedStrikeMove), new SingleAttackIntent(GuardedStrikeDamage), new DefendIntent());
		moveState.FollowUpState = moveState2;
		moveState2.FollowUpState = moveState3;
		moveState3.FollowUpState = moveState4;
		moveState4.FollowUpState = moveState5;
		moveState5.FollowUpState = moveState;
		list.Add(moveState);
		list.Add(moveState2);
		list.Add(moveState3);
		list.Add(moveState4);
		list.Add(moveState5);
		return new MonsterMoveStateMachine(list, moveState);
	}

	public override void AfterAddedToRoom()
	{
		base.AfterAddedToRoom();
		PowerCmd.Apply<BackAttackLeftPower>(base.Creature, 1m, base.Creature, null);
		PowerCmd.Apply<CrabRagePower>(base.Creature, 1m, base.Creature, null);
	}

	public override void AfterCurrentHpChanged(Creature creature, decimal delta)
	{
		if (creature != base.Creature || delta >= 0m)
		{
			return;
		}
		return;
	}

	public override void BeforeDeath(Creature creature)
	{
		if (creature != base.Creature)
		{
			return;
		}
		return;
	}

	private void ThrashMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(ThrashDamage).FromMonster(this)
			.Execute(null);
	}

	private void BugStingMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(BugStingDamage).WithHitCount(BugStingTimes).FromMonster(this)
			.Execute(null);
		PowerCmd.Apply<WeakPower>(targets, 2m, base.Creature, null);
		PowerCmd.Apply<FrailPower>(targets, 2m, base.Creature, null);
	}

	private void AdaptMove(IReadOnlyList<Creature> targets)
	{
		PowerCmd.Apply<StrengthPower>(base.Creature, AdaptStrengthGain, base.Creature, null);
	}

	private void EnlargingStrikeMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(EnlargingStrikeDamage).FromMonster(this)
			.Execute(null);
	}

	private void GuardedStrikeMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(GuardedStrikeDamage).FromMonster(this)
			.Execute(null);
		CreatureCmd.GainBlock(base.Creature, 18m, ValueProp.Move, null);
	}
}

