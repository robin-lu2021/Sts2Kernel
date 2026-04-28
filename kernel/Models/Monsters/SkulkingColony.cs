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

public sealed class SkulkingColony : MonsterModel
{
	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 75, 70);

	public override int MaxInitialHp => MinInitialHp;

	private int SmashDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 13, 12);

	private int InertiaDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 11, 9);

	private int ZoomDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 16, 14);

	private int ZoomBlock => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 13, 10);

	private int PiercingStabsDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 8, 7);

	private int PiercingStabsRepeat => 2;

	private int InertiaStrengthGain => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 3, 2);

	public override void AfterAddedToRoom()
	{
		base.AfterAddedToRoom();
		PowerCmd.Apply<HardenedShellPower>(base.Creature, 15m, base.Creature, null);
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("SMASH_MOVE", SmashMove, new SingleAttackIntent(SmashDamage));
		MoveState moveState2 = new MoveState("ZOOM_MOVE", ZoomMove, new SingleAttackIntent(ZoomDamage), new DefendIntent());
		MoveState moveState3 = new MoveState("INERTIA_MOVE", InertiaMove, new SingleAttackIntent(InertiaDamage), new BuffIntent());
		MoveState moveState4 = new MoveState("PIERCING_STABS_MOVE", PiercingStabsMove, new MultiAttackIntent(PiercingStabsDamage, PiercingStabsRepeat));
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

	private void InertiaMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(InertiaDamage).FromMonster(this)
			.Execute(null);
		PowerCmd.Apply<StrengthPower>(base.Creature, InertiaStrengthGain, base.Creature, null);
	}

	private void PiercingStabsMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(PiercingStabsDamage).WithHitCount(PiercingStabsRepeat).FromMonster(this)
			.Execute(null);
	}

	private void ZoomMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(ZoomDamage).FromMonster(this)
			.Execute(null);
		CreatureCmd.GainBlock(base.Creature, ZoomBlock, ValueProp.Move, null);
	}

	private void SmashMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(SmashDamage).FromMonster(this)
			.Execute(null);
	}
}

