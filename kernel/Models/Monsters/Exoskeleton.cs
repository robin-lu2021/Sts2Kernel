using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace MegaCrit.Sts2.Core.Models.Monsters;

public sealed class Exoskeleton : MonsterModel
{
	private const string _buffTrigger = "Buff";

	private const int _buffAmount = 2;

	private const string _heavyAttackTrigger = "HeavyAttack";

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 25, 24);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 29, 28);

	private int SkitterDamage => 1;

	private int SkitterRepeats => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 4, 3);

	private int MandiblesDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 9, 8);

	public override void AfterAddedToRoom()
	{
		base.AfterAddedToRoom();
		PowerCmd.Apply<HardToKillPower>(base.Creature, 9m, base.Creature, null);
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("SKITTER_MOVE", SyncMove(SkitterMove), new MultiAttackIntent(SkitterDamage, SkitterRepeats));
		MoveState moveState2 = new MoveState("MANDIBLE_MOVE", SyncMove(MandiblesMove), new SingleAttackIntent(MandiblesDamage));
		MoveState moveState3 = new MoveState("ENRAGE_MOVE", SyncMove(EnrageMove), new BuffIntent());
		RandomBranchState randomBranchState = new RandomBranchState("RAND");
		randomBranchState.AddBranch(moveState, MoveRepeatType.CannotRepeat, 1f);
		randomBranchState.AddBranch(moveState2, MoveRepeatType.CannotRepeat, 1f);
		ConditionalBranchState conditionalBranchState = new ConditionalBranchState("INIT_MOVE");
		conditionalBranchState.AddState(moveState, () => base.Creature.SlotName == "first");
		conditionalBranchState.AddState(moveState2, () => base.Creature.SlotName == "second");
		conditionalBranchState.AddState(moveState3, () => base.Creature.SlotName == "third");
		conditionalBranchState.AddState(randomBranchState, () => base.Creature.SlotName == "fourth");
		moveState.FollowUpState = randomBranchState;
		moveState2.FollowUpState = moveState3;
		moveState3.FollowUpState = randomBranchState;
		list.Add(conditionalBranchState);
		list.Add(randomBranchState);
		list.Add(moveState);
		list.Add(moveState2);
		list.Add(moveState3);
		return new MonsterMoveStateMachine(list, conditionalBranchState);
	}

	private void SkitterMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(SkitterDamage).WithHitCount(SkitterRepeats).FromMonster(this)
			.Execute(null);
	}

	private void MandiblesMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(MandiblesDamage).FromMonster(this)
			.Execute(null);
	}

	private void EnrageMove(IReadOnlyList<Creature> targets)
	{
		PowerCmd.Apply<StrengthPower>(base.Creature, 2m, base.Creature, null);
	}

	
}

