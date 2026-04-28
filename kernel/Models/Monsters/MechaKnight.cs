using System.Collections.Generic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core.Models.Monsters;

public sealed class MechaKnight : MonsterModel
{
	private const int _flamethrowerCardCount = 4;

	private const int _windupBlock = 15;

	private const string _windUpTrigger = "windUp";

	private const string _flameAttackTrigger = "flamethrower";

	private const string _chargeTrigger = "charge";

	private bool _isWoundUp;

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 320, 300);

	public override int MaxInitialHp => MinInitialHp;

	private static int ChargeDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 30, 25);

	private static int HeavyCleaveDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 40, 35);

	private bool IsWoundUp
	{
		get
		{
			return _isWoundUp;
		}
		set
		{
			AssertMutable();
			_isWoundUp = value;
		}
	}

	public override void AfterAddedToRoom()
	{
		base.AfterAddedToRoom();
		PowerCmd.Apply<ArtifactPower>(base.Creature, 3m, base.Creature, null);
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("CHARGE_MOVE", SyncMove(ChargeMove), new SingleAttackIntent(ChargeDamage));
		MoveState moveState2 = new MoveState("FLAMETHROWER_MOVE", SyncMove(FlamethrowerMove), new StatusIntent(4));
		MoveState moveState3 = new MoveState("WINDUP_MOVE", SyncMove(WindupMove), new DefendIntent(), new BuffIntent());
		MoveState moveState4 = new MoveState("HEAVY_CLEAVE_MOVE", SyncMove(HeavyCleaveMove), new SingleAttackIntent(HeavyCleaveDamage));
		moveState.FollowUpState = moveState2;
		moveState2.FollowUpState = moveState3;
		moveState3.FollowUpState = moveState4;
		moveState4.FollowUpState = moveState2;
		list.Add(moveState);
		list.Add(moveState4);
		list.Add(moveState3);
		list.Add(moveState2);
		return new MonsterMoveStateMachine(list, moveState);
	}

	private void ChargeMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(ChargeDamage).FromMonster(this)
			.Execute(null);
	}

	private void HeavyCleaveMove(IReadOnlyList<Creature> targets)
	{
		IsWoundUp = false;
		DamageCmd.Attack(HeavyCleaveDamage).FromMonster(this)
			.Execute(null);
	}

	private void WindupMove(IReadOnlyList<Creature> targets)
	{
		IsWoundUp = true;
		CreatureCmd.GainBlock(base.Creature, 15m, ValueProp.Move, null);
		PowerCmd.Apply<StrengthPower>(base.Creature, 5m, base.Creature, null);
	}

	private void FlamethrowerMove(IReadOnlyList<Creature> targets)
	{
		CardPileCmd.AddToCombatAndPreview<Burn>(targets, PileType.Hand, 4, addedByPlayer: false);
	}

	
}

