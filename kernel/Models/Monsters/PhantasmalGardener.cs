using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace MegaCrit.Sts2.Core.Models.Monsters;

public sealed class PhantasmalGardener : MonsterModel
{
	private int _enlargeTriggers;

	private const string _attackMultiTrigger = "AttackMulti";

	public const string blockStartTrigger = "BlockStart";

	public const string blockEndTrigger = "BlockEnd";

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 27, 26);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 32, 31);

	private int BiteDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 5, 5);

	private int LashDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 7, 7);

	private int FlailDamage => 1;

	private int FlailRepeat => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 3, 3);

	private int EnlargeStr => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 3, 2);

	private int SkittishAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 7, 6);

	public int EnlargeTriggers
	{
		get
		{
			return _enlargeTriggers;
		}
		set
		{
			AssertMutable();
			_enlargeTriggers = value;
		}
	}

	public override bool ShouldFadeAfterDeath => false;

	public float CurrentScale { get; private set; } = 1f;

	public override void AfterAddedToRoom()
	{
		base.AfterAddedToRoom();
		PowerCmd.Apply<SkittishPower>(base.Creature, SkittishAmount, base.Creature, null);
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("BITE_MOVE", SyncMove(BiteMove), new SingleAttackIntent(BiteDamage));
		MoveState moveState2 = new MoveState("LASH_MOVE", SyncMove(LashMove), new SingleAttackIntent(LashDamage));
		MoveState moveState3 = new MoveState("FLAIL_MOVE", SyncMove(FlailMove), new MultiAttackIntent(FlailDamage, FlailRepeat));
		MoveState moveState4 = new MoveState("ENLARGE_MOVE", SyncMove(EnlargeMove), new BuffIntent());
		ConditionalBranchState conditionalBranchState = new ConditionalBranchState("INIT_MOVE");
		conditionalBranchState.AddState(moveState3, () => base.Creature.SlotName == "first");
		conditionalBranchState.AddState(moveState, () => base.Creature.SlotName == "second");
		conditionalBranchState.AddState(moveState2, () => base.Creature.SlotName == "third");
		conditionalBranchState.AddState(moveState4, () => base.Creature.SlotName == "fourth");
		moveState.FollowUpState = moveState2;
		moveState2.FollowUpState = moveState3;
		moveState3.FollowUpState = moveState4;
		moveState4.FollowUpState = moveState;
		list.Add(moveState);
		list.Add(moveState2);
		list.Add(moveState4);
		list.Add(moveState3);
		return new MonsterMoveStateMachine(list, conditionalBranchState);
	}

	private void BiteMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(BiteDamage).FromMonster(this)
			.Execute(null);
	}

	private void LashMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(LashDamage).FromMonster(this)
			.Execute(null);
	}

	private void FlailMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(FlailDamage).WithHitCount(FlailRepeat).OnlyPlayAnimOnce()
			.FromMonster(this)
			.Execute(null);
	}

	private void EnlargeMove(IReadOnlyList<Creature> targets)
	{
		PowerCmd.Apply<StrengthPower>(base.Creature, EnlargeStr, base.Creature, null);
		EnlargeTriggers++;
		CurrentScale = 1f + 0.1f * (float)Math.Log(EnlargeTriggers + 1);
	}
}

