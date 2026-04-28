using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.TestSupport;

namespace MegaCrit.Sts2.Core.Models.Monsters;

public sealed class KinFollower : MonsterModel
{
	private const string _slashTrigger = "SlashTrigger";

	private const string _boomerangTrigger = "BoomerangTrigger";

	private const int _boomerangRepeat = 2;

	private bool _startsWithDance;

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 62, 58);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 63, 59);

	private int QuickSlashDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 5, 5);

	private int BoomerangDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 2, 2);

	private int DanceStrength => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 3, 2);

	public bool StartsWithDance
	{
		get
		{
			return _startsWithDance;
		}
		set
		{
			AssertMutable();
			_startsWithDance = value;
		}
	}

	public override void AfterAddedToRoom()
	{
		base.AfterAddedToRoom();
		PowerCmd.Apply<MinionPower>(base.Creature, 1m, base.Creature, null);
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("QUICK_SLASH_MOVE", SyncMove(QuickSlashMove), new SingleAttackIntent(QuickSlashDamage));
		MoveState moveState2 = new MoveState("BOOMERANG_MOVE", SyncMove(BoomerangMove), new MultiAttackIntent(BoomerangDamage, 2));
		MoveState moveState3 = new MoveState("POWER_DANCE_MOVE", SyncMove(PowerDanceMove), new BuffIntent());
		moveState.FollowUpState = moveState2;
		moveState2.FollowUpState = moveState3;
		moveState3.FollowUpState = moveState;
		list.Add(moveState);
		list.Add(moveState2);
		list.Add(moveState3);
		MoveState initialState = (StartsWithDance ? moveState3 : moveState);
		return new MonsterMoveStateMachine(list, initialState);
	}

	private void QuickSlashMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(QuickSlashDamage).FromMonster(this)
			.Execute(null);
	}

	private void PowerDanceMove(IReadOnlyList<Creature> targets)
	{
		PowerCmd.Apply<StrengthPower>(base.Creature, DanceStrength, base.Creature, null);
	}

	private void BoomerangMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(BoomerangDamage).WithHitCount(2).FromMonster(this)
			.Execute(null);
	}
}

