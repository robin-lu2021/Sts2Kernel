using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core.Models.Monsters;

public sealed class CubexConstruct : MonsterModel
{
	private const string _burrowTrigger = "Burrow";

	private const string _chargeTrigger = "Charge";

	private const string _attackEndTrigger = "AttackEnd";

	private const string _chargeStartAnimId = "charge_start";

	private const int _expelRepeats = 2;

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 70, 65);

	public override int MaxInitialHp => MinInitialHp;

	public override string BestiaryAttackAnimId => "charge_start";

	private int BlastDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 8, 7);

	private int ExpelDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 6, 5);

	public override void AfterAddedToRoom()
	{
		base.AfterAddedToRoom();
		CreatureCmd.GainBlock(base.Creature, 13m, ValueProp.Move, null);
		PowerCmd.Apply<ArtifactPower>(base.Creature, 1m, base.Creature, null);
		base.Creature.CurrentHpChanged += OnHpChanged;
	}

	public override void BeforeRemovedFromRoom()
	{
		base.Creature.CurrentHpChanged -= OnHpChanged;
	}

	public void OnHpChanged(int oldHp, int newHp)
	{
		if (newHp < oldHp)
		{
			;
		}
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("CHARGE_UP_MOVE", SyncMove(ChargeUpMove), new BuffIntent());
		MoveState moveState2 = new MoveState("REPEATER_MOVE", SyncMove(RepeaterBlastMove), new SingleAttackIntent(BlastDamage), new BuffIntent());
		MoveState moveState3 = new MoveState("REPEATER_MOVE_2", SyncMove(RepeaterBlastMove), new SingleAttackIntent(BlastDamage), new BuffIntent());
		MoveState moveState4 = new MoveState("EXPEL_BLAST", SyncMove(ExpelBlastMove), new MultiAttackIntent(ExpelDamage, 2));
		MoveState moveState5 = new MoveState("SUBMERGE_MOVE", SyncMove(SubmergeMove), new DefendIntent());
		moveState.FollowUpState = moveState2;
		moveState2.FollowUpState = moveState3;
		moveState3.FollowUpState = moveState4;
		moveState4.FollowUpState = moveState2;
		moveState5.FollowUpState = moveState;
		list.Add(moveState);
		list.Add(moveState2);
		list.Add(moveState3);
		list.Add(moveState4);
		list.Add(moveState5);
		return new MonsterMoveStateMachine(list, moveState);
	}

	private void ChargeUpMove(IReadOnlyList<Creature> targets)
	{
		PowerCmd.Apply<StrengthPower>(base.Creature, 2m, base.Creature, null);
	}

	private void RepeaterBlastMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(BlastDamage).FromMonster(this)
			.Execute(null);
		PowerCmd.Apply<StrengthPower>(base.Creature, 2m, base.Creature, null);
	}

	private void ExpelBlastMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(ExpelDamage).WithHitCount(2).FromMonster(this)
			.Execute(null);
	}

	private void SubmergeMove(IReadOnlyList<Creature> targets)
	{
		CreatureCmd.GainBlock(base.Creature, 15m, ValueProp.Move, null);
	}

	
}

