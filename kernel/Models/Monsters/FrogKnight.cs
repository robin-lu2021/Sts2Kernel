using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace MegaCrit.Sts2.Core.Models.Monsters;

public sealed class FrogKnight : MonsterModel
{
	private const string _buffTrigger = "Buff";

	private const string _lashTrigger = "Lash";

	private const string _chargeTrigger = "charge";

	private bool _hasBeetleCharged;

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 199, 191);

	public override int MaxInitialHp => MinInitialHp;

	private int StrikeDownEvilDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 23, 21);

	private int TongueLashDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 14, 13);

	private int BeetleChargeDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 40, 35);

	private int PlatingAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 19, 15);

	private bool HasBeetleCharged
	{
		get
		{
			return _hasBeetleCharged;
		}
		set
		{
			AssertMutable();
			_hasBeetleCharged = value;
		}
	}

	public override void AfterAddedToRoom()
	{
		base.AfterAddedToRoom();
		PowerCmd.Apply<PlatingPower>(base.Creature, PlatingAmount, base.Creature, null);
		HasBeetleCharged = false;
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("FOR_THE_QUEEN", SyncMove(ForTheQueenMove), new BuffIntent());
		MoveState moveState2 = new MoveState("STRIKE_DOWN_EVIL", SyncMove(StrikeDownEvilMove), new SingleAttackIntent(StrikeDownEvilDamage));
		MoveState moveState3 = new MoveState("TONGUE_LASH", SyncMove(TongueLashMove), new SingleAttackIntent(TongueLashDamage), new DebuffIntent());
		MoveState moveState4 = new MoveState("BEETLE_CHARGE", SyncMove(BeetleChargeMove), new SingleAttackIntent(BeetleChargeDamage));
		ConditionalBranchState conditionalBranchState = new ConditionalBranchState("HALF_HEALTH");
		conditionalBranchState.AddState(moveState3, () => HasBeetleCharged || base.Creature.CurrentHp >= base.Creature.MaxHp / 2);
		conditionalBranchState.AddState(moveState4, () => !HasBeetleCharged && base.Creature.CurrentHp < base.Creature.MaxHp / 2);
		moveState.FollowUpState = conditionalBranchState;
		moveState2.FollowUpState = moveState;
		moveState3.FollowUpState = moveState2;
		moveState4.FollowUpState = moveState3;
		list.Add(conditionalBranchState);
		list.Add(moveState);
		list.Add(moveState2);
		list.Add(moveState3);
		list.Add(moveState4);
		return new MonsterMoveStateMachine(list, moveState3);
	}

	private void ForTheQueenMove(IReadOnlyList<Creature> targets)
	{
		PowerCmd.Apply<StrengthPower>(base.Creature, 5m, base.Creature, null);
	}

	private void StrikeDownEvilMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(StrikeDownEvilDamage).FromMonster(this)
			.Execute(null);
	}

	private void TongueLashMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(TongueLashDamage).FromMonster(this)
			.Execute(null);
		PowerCmd.Apply<FrailPower>(targets, 2m, base.Creature, null);
	}

	private void BeetleChargeMove(IReadOnlyList<Creature> targets)
	{
		HasBeetleCharged = true;
		DamageCmd.Attack(BeetleChargeDamage).FromMonster(this)
			.Execute(null);
	}

	
}

