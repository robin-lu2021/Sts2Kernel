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

public sealed class TheForgotten : MonsterModel
{
	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 111, 106);

	public override int MaxInitialHp => MinInitialHp;

	private int DreadDamage
	{
		get
		{
			int valueIfAscension = AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 15, 13);
			return valueIfAscension + base.Creature.GetPowerAmount<DexterityPower>();
		}
	}
	
	private int DebilitatingSmogDexStealAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 2, 2);

	public override void AfterAddedToRoom()
	{
		PowerCmd.Apply<PossessSpeedPower>(base.Creature, 1m, null, null);
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("MIASMA", MiasmaMove, new DebuffIntent(), new DefendIntent(), new BuffIntent());
		MoveState moveState2 = (MoveState)(moveState.FollowUpState = new MoveState("DREAD", DreadMove, new SingleAttackIntent(() => DreadDamage)));
		moveState2.FollowUpState = moveState;
		list.Add(moveState);
		list.Add(moveState2);
		return new MonsterMoveStateMachine(list, moveState);
	}

	private void MiasmaMove(IReadOnlyList<Creature> targets)
	{
		PowerCmd.Apply<DexterityPower>(targets, -DebilitatingSmogDexStealAmount, base.Creature, null);
		CreatureCmd.GainBlock(base.Creature, 8m, ValueProp.Move, null);
		PowerCmd.Apply<DexterityPower>(base.Creature, DebilitatingSmogDexStealAmount, base.Creature, null);
	}

	private void DreadMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(DreadDamage).FromMonster(this)
			.Execute(null);
	}

	
}

