using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.TestSupport;

namespace MegaCrit.Sts2.Core.Models.Monsters;

public sealed class Myte : MonsterModel
{
	private const int _toxicCount = 2;

	private const string _suckTrigger = "Suck";

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 64, 61);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 69, 67);

	private int BiteDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 15, 13);

	private int SuckDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 6, 4);

	private int SuckStrength => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 3, 2);

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("TOXIC_MOVE", SyncMove(ToxicMove), new StatusIntent(2));
		MoveState moveState2 = new MoveState("BITE_MOVE", SyncMove(BiteMove), new SingleAttackIntent(BiteDamage));
		MoveState moveState3 = new MoveState("SUCK_MOVE", SyncMove(SuckMove), new SingleAttackIntent(SuckDamage), new BuffIntent());
		ConditionalBranchState conditionalBranchState = new ConditionalBranchState("INIT_MOVE");
		conditionalBranchState.AddState(moveState, () => base.Creature.SlotName == "first");
		conditionalBranchState.AddState(moveState3, () => base.Creature.SlotName == "second");
		moveState.FollowUpState = moveState2;
		moveState2.FollowUpState = moveState3;
		moveState3.FollowUpState = moveState;
		list.Add(moveState);
		list.Add(moveState2);
		list.Add(moveState3);
		return new MonsterMoveStateMachine(list, conditionalBranchState);
	}

	private void ToxicMove(IReadOnlyList<Creature> targets)
	{
		CardPileCmd.AddToCombatAndPreview<Toxic>(targets, PileType.Hand, 2, addedByPlayer: false);
	}

	private void BiteMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(BiteDamage).FromMonster(this)
			.Execute(null);
	}

	private void SuckMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(SuckDamage).FromMonster(this)
			.Execute(null);
		PowerCmd.Apply<StrengthPower>(base.Creature, SuckStrength, base.Creature, null);
	}

	
}

