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

public sealed class InfestedPrism : MonsterModel
{
	private const string _attackBlockTrigger = "AttackBlock";

	private const string _attackDoubleTrigger = "AttackDouble";

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 215, 200);

	public override int MaxInitialHp => MinInitialHp;

	private int JabDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 24, 22);

	private int PulsatePowerAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 5, 4);

	private int PulsateBlock => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 22, 20);

	private int RadiateDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 18, 16);

	private int RadiateBlock => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 18, 16);

	private int WhirlwindDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 10, 9);

	private int WhirlwindRepeat => 3;

	public override void AfterAddedToRoom()
	{
		base.AfterAddedToRoom();
		PowerCmd.Apply<VitalSparkPower>(base.Creature, 1m, base.Creature, null);
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("JAB_MOVE", SyncMove(JabMove), new SingleAttackIntent(JabDamage));
		MoveState moveState2 = new MoveState("RADIATE_MOVE", SyncMove(RadiateMove), new SingleAttackIntent(RadiateDamage), new DefendIntent());
		MoveState moveState3 = new MoveState("WHIRLWIND_MOVE", SyncMove(WhirlwindMove), new MultiAttackIntent(WhirlwindDamage, WhirlwindRepeat));
		MoveState moveState4 = new MoveState("PULSATE_MOVE", SyncMove(PulsateMove), new BuffIntent(), new DefendIntent());
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

	private void JabMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(JabDamage).FromMonster(this)
			.Execute(null);
	}

	private void RadiateMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(RadiateDamage).FromMonster(this)
			.Execute(null);
		CreatureCmd.GainBlock(base.Creature, RadiateBlock, ValueProp.Move, null);
	}

	private void WhirlwindMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(WhirlwindDamage).WithHitCount(WhirlwindRepeat).FromMonster(this)
			.Execute(null);
	}

	private void PulsateMove(IReadOnlyList<Creature> targets)
	{
		CreatureCmd.GainBlock(base.Creature, PulsateBlock, ValueProp.Move, null);
		PowerCmd.Apply<StrengthPower>(base.Creature, PulsatePowerAmount, base.Creature, null);
	}

	
}

