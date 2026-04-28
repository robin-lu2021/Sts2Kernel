using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Random;

namespace MegaCrit.Sts2.Core.Models.Monsters;

public sealed class Toadpole : MonsterModel
{
	private bool _isFront;

	private const string _attackSingleTrigger = "AttackSingle";

	private const string _attackTripleTrigger = "AttackTriple";

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 22, 21);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 26, 25);

	public bool IsFront
	{
		get
		{
			return _isFront;
		}
		set
		{
			AssertMutable();
			_isFront = value;
		}
	}

	private int SpikeSpitDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 4, 3);

	private int SpikeSpitRepeat => 3;

	private int WhirlDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 8, 7);

	private int SpikenAmount => 2;

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("SPIKE_SPIT_MOVE", SyncMove(SpikeSpitMove), new MultiAttackIntent(SpikeSpitDamage, SpikeSpitRepeat));
		MoveState moveState2 = new MoveState("WHIRL_MOVE", SyncMove(WhirlMove), new SingleAttackIntent(WhirlDamage));
		MoveState moveState3 = new MoveState("SPIKEN_MOVE", SyncMove(SpikenMove), new BuffIntent());
		ConditionalBranchState conditionalBranchState = new ConditionalBranchState("INIT_MOVE");
		moveState2.FollowUpState = moveState3;
		moveState3.FollowUpState = moveState;
		moveState.FollowUpState = moveState2;
		conditionalBranchState.AddState(moveState2, () => !((Toadpole)base.Creature.Monster).IsFront);
		conditionalBranchState.AddState(moveState3, () => ((Toadpole)base.Creature.Monster).IsFront);
		list.Add(conditionalBranchState);
		list.Add(moveState3);
		list.Add(moveState);
		list.Add(moveState2);
		return new MonsterMoveStateMachine(list, conditionalBranchState);
	}

	private void SpikeSpitMove(IReadOnlyList<Creature> targets)
	{
		PowerCmd.Apply<ThornsPower>(base.Creature, -SpikenAmount, base.Creature, null);
		DamageCmd.Attack(SpikeSpitDamage).WithHitCount(SpikeSpitRepeat).FromMonster(this)
			.Execute(null);
	}

	private void WhirlMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(WhirlDamage).FromMonster(this)
			.Execute(null);
	}

	private void SpikenMove(IReadOnlyList<Creature> targets)
	{
		PowerCmd.Apply<ThornsPower>(base.Creature, SpikenAmount, base.Creature, null);
	}

	
}

