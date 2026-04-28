using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.TestSupport;

namespace MegaCrit.Sts2.Core.Models.Monsters;

public sealed class Flyconid : MonsterModel
{
	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 51, 47);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 53, 49);

	private int SmashDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 12, 11);

	private int SporeDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 9, 8);

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("VULNERABLE_SPORES_MOVE", SyncMove(VulnerableSporesMove), new DebuffIntent());
		MoveState moveState2 = new MoveState("FRAIL_SPORES_MOVE", SyncMove(FrailSporesMove), new SingleAttackIntent(SporeDamage), new DebuffIntent());
		MoveState moveState3 = new MoveState("SMASH_MOVE", SyncMove(SmashMove), new SingleAttackIntent(SmashDamage));
		RandomBranchState randomBranchState = new RandomBranchState("RAND");
		RandomBranchState randomBranchState2 = new RandomBranchState("INITIAL");
		moveState.FollowUpState = randomBranchState;
		moveState2.FollowUpState = randomBranchState;
		moveState3.FollowUpState = randomBranchState;
		randomBranchState.AddBranch(moveState, 3, MoveRepeatType.CannotRepeat);
		randomBranchState.AddBranch(moveState2, 2, MoveRepeatType.CannotRepeat);
		randomBranchState.AddBranch(moveState3, MoveRepeatType.CannotRepeat);
		randomBranchState2.AddBranch(moveState2, 2, MoveRepeatType.CannotRepeat);
		randomBranchState2.AddBranch(moveState3, MoveRepeatType.CannotRepeat);
		list.Add(moveState);
		list.Add(moveState2);
		list.Add(moveState3);
		list.Add(randomBranchState);
		list.Add(randomBranchState2);
		return new MonsterMoveStateMachine(list, randomBranchState2);
	}

	private void VulnerableSporesMove(IReadOnlyList<Creature> targets)
	{
		PowerCmd.Apply<VulnerablePower>(targets, 2m, base.Creature, null);
	}

	private void FrailSporesMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(SporeDamage).FromMonster(this)
			.Execute(null);
		PowerCmd.Apply<FrailPower>(targets, 2m, base.Creature, null);
	}

	private void SmashMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(SmashDamage).FromMonster(this)
			.Execute(null);
	}
}

