using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.TestSupport;

namespace MegaCrit.Sts2.Core.Models.Monsters;

public sealed class Vantom : MonsterModel
{
	private const string _vantomCustomTrackName = "vantom_progress";

	private const int _inkyLanceRepeat = 2;

	private const int _dismemberWounds = 3;

	private const int _prepareStrength = 2;

	private const string _chargeUpTrigger = "CHARGE_UP";

	private const string _buffTrigger = "BUFF";

	private const string _debuffTrigger = "DEBUFF";

	private const string _heavyAttackTrigger = "ATTACK_HEAVY";

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 183, 173);

	public override int MaxInitialHp => MinInitialHp;

	private int InkBlotDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 8, 7);

	private int InkyLanceDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 7, 6);

	private int DismemberDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 30, 27);

	public override bool ShouldDisappearFromDoom => false;

	public override void AfterAddedToRoom()
	{
		base.AfterAddedToRoom();
		PowerCmd.Apply<SlipperyPower>(base.Creature, 9m, base.Creature, null);
	}

	public override void AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
	{
		return;
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("INK_BLOT_MOVE", SyncMove(InkBlotMove), new SingleAttackIntent(InkBlotDamage));
		MoveState moveState2 = new MoveState("INKY_LANCE_MOVE", SyncMove(InkyLanceMove), new MultiAttackIntent(InkyLanceDamage, 2));
		MoveState moveState3 = new MoveState("DISMEMBER_MOVE", SyncMove(DismemberMove), new SingleAttackIntent(DismemberDamage), new StatusIntent(3));
		MoveState moveState4 = new MoveState("PREPARE_MOVE", SyncMove(PrepareMove), new BuffIntent());
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

	private void InkBlotMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(InkBlotDamage).FromMonster(this)
			.Execute(null);
	}

	private void InkyLanceMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(InkyLanceDamage).WithHitCount(2).FromMonster(this)
			.Execute(null);
	}

	private void DismemberMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(DismemberDamage).FromMonster(this).WithNoAttackerAnim()
			.Execute(null);
		CardPileCmd.AddToCombatAndPreview<Wound>(targets, PileType.Discard, 3, addedByPlayer: false);
	}

	private void PrepareMove(IReadOnlyList<Creature> targets)
	{
		PowerCmd.Apply<StrengthPower>(base.Creature, 2m, base.Creature, null);
	}
}

