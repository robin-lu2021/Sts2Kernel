using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Random;

namespace MegaCrit.Sts2.Core.Models.Monsters;

public sealed class CeremonialBeast : MonsterModel
{
	private const string _plowTrigger = "Plow";

	private const string _plowEndTrigger = "EndPlow";

	private const string _stunTrigger = "Stun";

	private const string _unStunTrigger = "Unstun";

	private const string _plowHitTrigger = "PlowHit";

	private bool _isStunnedByPlowRemoval;

	private bool _inMidCharge;

	private MoveState _beastCryState;

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 262, 252);

	public override int MaxInitialHp => MinInitialHp;

	private int PlowAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 160, 150);

	private int PlowDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 20, 18);

	private int PlowStrength => 2;

	private int StompDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 17, 15);

	private int CrushDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 19, 17);

	private int CrushStrength => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 4, 3);

	public override bool ShouldDisappearFromDoom => false;

	public override bool ShouldFadeAfterDeath => false;

	private bool IsStunnedByPlowRemoval
	{
		get
		{
			return _isStunnedByPlowRemoval;
		}
		set
		{
			AssertMutable();
			_isStunnedByPlowRemoval = value;
		}
	}

	private bool ShouldPlayRegularHurtAnim
	{
		get
		{
			if (!IsStunnedByPlowRemoval)
			{
				return !InMidCharge;
			}
			return false;
		}
	}

	private bool InMidCharge
	{
		get
		{
			return _inMidCharge;
		}
		set
		{
			AssertMutable();
			_inMidCharge = value;
		}
	}

	public MoveState BeastCryState
	{
		get
		{
			return _beastCryState;
		}
		set
		{
			AssertMutable();
			_beastCryState = value;
		}
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("STAMP_MOVE", StampMove, new BuffIntent());
		MoveState moveState2 = new MoveState("PLOW_MOVE", PlowMove, new SingleAttackIntent(PlowDamage), new BuffIntent());
		MoveState moveState3 = new MoveState("STUN_MOVE", StunnedMove, new StunIntent());
		BeastCryState = new MoveState("BEAST_CRY_MOVE", BeastCryMove, new DebuffIntent());
		MoveState moveState4 = new MoveState("STOMP_MOVE", StompMove, new SingleAttackIntent(StompDamage));
		MoveState moveState5 = new MoveState("CRUSH_MOVE", CrushMove, new SingleAttackIntent(CrushDamage), new BuffIntent());
		moveState.FollowUpState = moveState2;
		moveState2.FollowUpState = moveState2;
		moveState3.FollowUpState = BeastCryState;
		BeastCryState.FollowUpState = moveState4;
		moveState4.FollowUpState = moveState5;
		moveState5.FollowUpState = BeastCryState;
		list.Add(moveState2);
		list.Add(moveState);
		list.Add(moveState3);
		list.Add(BeastCryState);
		list.Add(moveState4);
		list.Add(moveState5);
		return new MonsterMoveStateMachine(list, moveState);
	}

	private void StampMove(IReadOnlyList<Creature> targets)
	{
		PowerCmd.Apply<PlowPower>(base.Creature, PlowAmount, base.Creature, null);
	}

	private void PlowMove(IReadOnlyList<Creature> targets)
	{
		InMidCharge = true;
		using (IEnumerator<Creature> enumerator = targets.GetEnumerator())
		{
			if (enumerator.MoveNext())
			{
				Creature current = enumerator.Current;
			}
		}
		DamageCmd.Attack(PlowDamage).FromMonster(this).WithNoAttackerAnim()
			.Execute(null);
		InMidCharge = false;
		PowerCmd.Apply<StrengthPower>(base.Creature, PlowStrength, base.Creature, null);
	}

	public void SetStunned()
	{
		IsStunnedByPlowRemoval = true;
	}

	public void StunnedMove(IReadOnlyList<Creature> targets)
	{
		IsStunnedByPlowRemoval = false;
	}

	private void BeastCryMove(IReadOnlyList<Creature> targets)
	{
		PowerCmd.Apply<RingingPower>(targets, 1m, base.Creature, null);
	}

	private void StompMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(StompDamage).FromMonster(this)
			.Execute(null);
	}

	private void CrushMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(CrushDamage).FromMonster(this)
			.Execute(null);
		PowerCmd.Apply<StrengthPower>(base.Creature, CrushStrength, base.Creature, null);
	}
}

