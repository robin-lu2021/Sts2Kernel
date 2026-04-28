using System.Collections.Generic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace MegaCrit.Sts2.Core.Models.Monsters;

public sealed class Rocket : MonsterModel
{
	public override bool ShouldFadeAfterDeath => false;

	public override bool ShouldDisappearFromDoom => false;

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 209, 199);

	public override int MaxInitialHp => MinInitialHp;

	private int TargetingReticleDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 4, 3);

	private int PrecisionBeamDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 20, 18);

	private int LaserDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 35, 31);

	private int ChargeUpStrengthGain => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 3, 2);

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("TARGETING_RETICLE_MOVE", SyncMove(TargetingReticleMove), new SingleAttackIntent(TargetingReticleDamage));
		MoveState moveState2 = new MoveState("PRECISION_BEAM_MOVE", SyncMove(PrecisionBeamMove), new SingleAttackIntent(PrecisionBeamDamage));
		MoveState moveState3 = new MoveState("CHARGE_UP_MOVE", SyncMove(ChargeUpMove), new BuffIntent());
		MoveState moveState4 = new MoveState("LASER_MOVE", SyncMove(LaserMove), new SingleAttackIntent(LaserDamage));
		MoveState moveState5 = new MoveState("RECHARGE_MOVE", SyncMove(RechargeMove), new SleepIntent());
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
		PowerCmd.Apply<SurroundedPower>(base.CombatState.GetOpponentsOf(base.Creature), 1m, base.Creature, null);
		PowerCmd.Apply<BackAttackRightPower>(base.Creature, 1m, base.Creature, null);
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

	private void TargetingReticleMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(TargetingReticleDamage).FromMonster(this)
			.Execute(null);
	}

	private void PrecisionBeamMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(PrecisionBeamDamage).FromMonster(this)
			.Execute(null);
	}

	private void ChargeUpMove(IReadOnlyList<Creature> targets)
	{
		PowerCmd.Apply<StrengthPower>(base.Creature, ChargeUpStrengthGain, base.Creature, null);
	}

	private void LaserMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(LaserDamage).FromMonster(this)
			.Execute(null);
	}

	private void RechargeMove(IReadOnlyList<Creature> targets)
	{
		;
	}
}

