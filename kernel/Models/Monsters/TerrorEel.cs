using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace MegaCrit.Sts2.Core.Models.Monsters;

public sealed class TerrorEel : MonsterModel
{
	private const string _thrashMoveId = "ThrashMove";

	private const int _hpNormal = 140;

	private const int _hpTough = 150;

	private MoveState _terrorState;

	private const string _attackTripleTrigger = "AttackTripleTrigger";

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 150, 140);

	public override int MaxInitialHp => MinInitialHp;

	private int ShriekAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 75, 70);

	private int CrashDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 18, 16);

	private int ThrashDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 4, 3);

	private int ThrashRepeat => 3;

	public MoveState TerrorState
	{
		get
		{
			return _terrorState;
		}
		private set
		{
			AssertMutable();
			_terrorState = value;
		}
	}

	public override void AfterAddedToRoom()
	{
		base.AfterAddedToRoom();
		PowerCmd.Apply<ShriekPower>(base.Creature, ShriekAmount, base.Creature, null);
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("CRASH_MOVE", SyncMove(CrashMove), new SingleAttackIntent(CrashDamage));
		MoveState moveState2 = new MoveState("ThrashMove", SyncMove(ThrashMove), new MultiAttackIntent(ThrashDamage, ThrashRepeat), new BuffIntent());
		MoveState moveState3 = new MoveState("STUN_MOVE", SyncMove(StunMove), new StunIntent());
		TerrorState = new MoveState("TERROR_MOVE", SyncMove(TerrorMove), new DebuffIntent());
		moveState.FollowUpState = moveState2;
		moveState2.FollowUpState = moveState;
		moveState3.FollowUpState = TerrorState;
		TerrorState.FollowUpState = moveState;
		list.Add(moveState);
		list.Add(moveState2);
		list.Add(moveState3);
		list.Add(TerrorState);
		return new MonsterMoveStateMachine(list, moveState);
	}

	private void CrashMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(CrashDamage).FromMonster(this)
			.Execute(null);
	}

	private void ThrashMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(ThrashDamage).WithHitCount(ThrashRepeat).FromMonster(this)
			.Execute(null);
		PowerCmd.Apply<VigorPower>(base.Creature, 6m, base.Creature, null);
	}

	private void StunMove(IReadOnlyList<Creature> targets)
	{
		return;
	}

	private void TerrorMove(IReadOnlyList<Creature> targets)
	{
		PowerCmd.Apply<VulnerablePower>(targets, 99m, base.Creature, null);
	}

	
}

