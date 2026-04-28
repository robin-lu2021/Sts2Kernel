using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace MegaCrit.Sts2.Core.Models.Monsters;

public sealed class OwlMagistrate : MonsterModel
{
	private const int _peckAssaultRepeat = 6;

	private const string _attackPeckAnimId = "attack_peck";

	private const string _takeOffTrigger = "TakeOff";

	private bool _isFlying;

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 243, 234);

	public override int MaxInitialHp => MinInitialHp;

	private int VerdictDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 36, 33);

	private int ScrutinyDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 17, 16);

	private int PeckAssaultDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 4, 4);

	public override string BestiaryAttackAnimId => "attack_peck";

	private bool IsFlying
	{
		get
		{
			return _isFlying;
		}
		set
		{
			AssertMutable();
			_isFlying = value;
		}
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("MAGISTRATE_SCRUTINY", SyncMove(MagistrateScrutinyMove), new SingleAttackIntent(ScrutinyDamage));
		MoveState moveState2 = new MoveState("PECK_ASSAULT", SyncMove(PeckAssaultMove), new MultiAttackIntent(PeckAssaultDamage, 6));
		MoveState moveState3 = new MoveState("JUDICIAL_FLIGHT", SyncMove(JudicialFlightMove), new BuffIntent());
		MoveState moveState4 = new MoveState("VERDICT", SyncMove(VerdictMove), new SingleAttackIntent(VerdictDamage), new DebuffIntent());
		moveState.FollowUpState = moveState2;
		moveState2.FollowUpState = moveState3;
		moveState3.FollowUpState = moveState4;
		moveState4.FollowUpState = moveState;
		list.Add(moveState);
		list.Add(moveState2);
		list.Add(moveState3);
		list.Add(moveState4);
		return new MonsterMoveStateMachine(list, moveState);
	}

	private void MagistrateScrutinyMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(ScrutinyDamage).FromMonster(this)
			.Execute(null);
	}

	private void PeckAssaultMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(PeckAssaultDamage).WithHitCount(6).FromMonster(this)
			.Execute(null);
	}

	private void JudicialFlightMove(IReadOnlyList<Creature> targets)
	{
		IsFlying = true;
		PowerCmd.Apply<SoarPower>(base.Creature, 1m, base.Creature, null);
	}

	private void VerdictMove(IReadOnlyList<Creature> targets)
	{
		IsFlying = false;
		DamageCmd.Attack(VerdictDamage).FromMonster(this)
			.Execute(null);
		PowerCmd.Apply<VulnerablePower>(targets, 4m, base.Creature, null);
		PowerCmd.Remove<SoarPower>(base.Creature);
	}

	
}

