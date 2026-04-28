using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.TestSupport;

namespace MegaCrit.Sts2.Core.Models.Monsters;

public sealed class WaterfallGiant : MonsterModel
{
	private const string _waterfallGiantTrackName = "waterfall_giant_progress";

	private const int _endCombatBgmFlag = 5;

	private const int _maxIntensityBgmFlag = 2;

	private const int _increaseIntensityBgmFlag = 1;

	private const int _increaseIntensityAmbienceFlag = 1;

	private const int _maxIntensityAmbienceFlag = 3;

	private const int _endAmbienceFlag = 2;

	private int _currentPressureGunDamage;

	private int _steamEruptionDamage;

	private MoveState _aboutToBlowState;

	private bool _isAboutToBlow;

	private int _pressureBuildupIdx;

	private const int _maxPressureBuildup = 6;

	private const string _attackBuffTrigger = "AttackBuff";

	private const string _attackDebuffTrigger = "AttackDebuff";

	private const string _healTrigger = "Heal";

	private const string _eruptTrigger = "Erupt";

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 250, 240);

	public override int MaxInitialHp => MinInitialHp;

	private int PressurizeAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 20, 15);

	private int StompDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 16, 15);

	private int RamDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 11, 10);

	private int PressureUpDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 14, 13);

	private int BasePressureGunDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 23, 20);

	public override bool ShouldDisappearFromDoom => !base.Creature.HasPower<SteamEruptionPower>();

	private int PressureGunIncrease => 5;

	private int SiphonHeal => 15;

	private int CurrentPressureGunDamage
	{
		get
		{
			return _currentPressureGunDamage;
		}
		set
		{
			AssertMutable();
			_currentPressureGunDamage = value;
		}
	}

	private int SteamEruptionDamage
	{
		get
		{
			return _steamEruptionDamage;
		}
		set
		{
			AssertMutable();
			_steamEruptionDamage = value;
		}
	}

	private MoveState AboutToBlowState
	{
		get
		{
			return _aboutToBlowState;
		}
		set
		{
			AssertMutable();
			_aboutToBlowState = value;
		}
	}

	private bool IsAboutToBlow
	{
		get
		{
			return _isAboutToBlow;
		}
		set
		{
			AssertMutable();
			_isAboutToBlow = value;
		}
	}

	private int PressureBuildupIdx
	{
		get
		{
			return _pressureBuildupIdx;
		}
		set
		{
			AssertMutable();
			_pressureBuildupIdx = value;
		}
	}

	public override bool ShouldFadeAfterDeath => PressureBuildupIdx == 0;

	public override void AfterAddedToRoom()
	{
		base.AfterAddedToRoom();
		CurrentPressureGunDamage = BasePressureGunDamage;
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("PRESSURIZE_MOVE", SyncMove(PressurizeMove), new BuffIntent());
		MoveState moveState2 = new MoveState("STOMP_MOVE", SyncMove(StompMove), new SingleAttackIntent(StompDamage), new DebuffIntent(), new BuffIntent());
		MoveState moveState3 = new MoveState("RAM_MOVE", SyncMove(RamMove), new SingleAttackIntent(RamDamage), new BuffIntent());
		MoveState moveState4 = new MoveState("SIPHON_MOVE", SyncMove(SiphonMove), new HealIntent(), new BuffIntent());
		MoveState moveState5 = new MoveState("PRESSURE_GUN_MOVE", SyncMove(PressureGunMove), new SingleAttackIntent(() => CurrentPressureGunDamage), new BuffIntent());
		MoveState moveState6 = new MoveState("PRESSURE_UP_MOVE", SyncMove(PressureUpMove), new SingleAttackIntent(PressureUpDamage), new BuffIntent());
		AboutToBlowState = new MoveState("ABOUT_TO_BLOW_MOVE", SyncMove(AboutToBlowMove), new StunIntent());
		MoveState moveState7 = new MoveState("EXPLODE_MOVE", SyncMove(ExplodeMove), new DeathBlowIntent(() => SteamEruptionDamage));
		moveState.FollowUpState = moveState2;
		moveState2.FollowUpState = moveState3;
		moveState3.FollowUpState = moveState4;
		moveState4.FollowUpState = moveState5;
		moveState5.FollowUpState = moveState6;
		moveState6.FollowUpState = moveState2;
		AboutToBlowState.FollowUpState = moveState7;
		moveState7.FollowUpState = moveState7;
		list.Add(moveState);
		list.Add(moveState2);
		list.Add(moveState3);
		list.Add(moveState4);
		list.Add(moveState5);
		list.Add(moveState6);
		list.Add(moveState7);
		list.Add(AboutToBlowState);
		return new MonsterMoveStateMachine(list, moveState);
	}

	private void PressurizeMove(IReadOnlyList<Creature> targets)
	{
		PowerCmd.Apply<SteamEruptionPower>(base.Creature, PressurizeAmount, base.Creature, null);
	}

	private void PressureUpMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(PressureUpDamage).FromMonster(this)
			.Execute(null);
		PowerCmd.Apply<SteamEruptionPower>(base.Creature, 3m, base.Creature, null);
	}

	private void StompMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(StompDamage).FromMonster(this)
			.Execute(null);
		PowerCmd.Apply<WeakPower>(targets, 1m, base.Creature, null);
		PowerCmd.Apply<SteamEruptionPower>(base.Creature, 3m, base.Creature, null);
	}

	private void RamMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(RamDamage).FromMonster(this)
			.Execute(null);
		PowerCmd.Apply<SteamEruptionPower>(base.Creature, 3m, base.Creature, null);
	}

	private void SiphonMove(IReadOnlyList<Creature> targets)
	{
		CreatureCmd.Heal(base.Creature, SiphonHeal * base.Creature.CombatState.Players.Count);
		PowerCmd.Apply<SteamEruptionPower>(base.Creature, 3m, base.Creature, null);
	}

	private void PressureGunMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(CurrentPressureGunDamage).FromMonster(this)
			.Execute(null);
		CurrentPressureGunDamage += PressureGunIncrease;
		PowerCmd.Apply<SteamEruptionPower>(base.Creature, 3m, base.Creature, null);
	}

	private void AboutToBlowMove(IReadOnlyList<Creature> targets)
	{
		SteamEruptionDamage = base.Creature.GetPowerAmount<SteamEruptionPower>();
		PowerCmd.Remove<SteamEruptionPower>(base.Creature);
		PressureBuildupIdx = 6;
	}

	private void ExplodeMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(SteamEruptionDamage).FromMonster(this)
			.Execute(null);
		CreatureCmd.Kill(base.Creature);
	}

	public void TriggerAboutToBlowState()
	{
		IsAboutToBlow = true;
		CreatureCmd.SetMaxAndCurrentHp(base.Creature, 999999999m);
		base.Creature.ShowsInfiniteHp = true;
		SetMoveImmediate(AboutToBlowState, forceTransition: true);
	}
}

