using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core.Models.Monsters;

public sealed class Tunneler : MonsterModel
{
	public const string biteMoveId = "BITE_MOVE";

	public const string stillDizzyMoveId = "DIZZY_MOVE";

	private const string _burrowedAttackTrigger = "BurrowAttack";

	public const string unburrowAttackTrigger = "UnburrowAttack";

	private const string _burrowTrigger = "Burrow";

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 92, 87);

	public override int MaxInitialHp => MinInitialHp;

	private int BiteDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 15, 13);

	private int BlockGain => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 37, 32);

	private int BelowDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 26, 23);

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("BITE_MOVE", SyncMove(BiteMove), new SingleAttackIntent(BiteDamage));
		MoveState moveState2 = new MoveState("BURROW_MOVE", SyncMove(BurrowMove), new BuffIntent(), new DefendIntent());
		MoveState moveState3 = new MoveState("BELOW_MOVE_1", SyncMove(BelowMove), new SingleAttackIntent(BelowDamage));
		MoveState moveState4 = new MoveState("DIZZY_MOVE", SyncMove(StillDizzyMove), new StunIntent());
		moveState.FollowUpState = moveState2;
		moveState2.FollowUpState = moveState3;
		moveState3.FollowUpState = moveState3;
		moveState4.FollowUpState = moveState;
		list.Add(moveState);
		list.Add(moveState2);
		list.Add(moveState3);
		list.Add(moveState4);
		return new MonsterMoveStateMachine(list, moveState);
	}

	private void BiteMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(BiteDamage).FromMonster(this)
			.Execute(null);
	}

	private void BurrowMove(IReadOnlyList<Creature> targets)
	{
		PowerCmd.Apply<BurrowedPower>(base.Creature, 1m, base.Creature, null);
		CreatureCmd.GainBlock(base.Creature, BlockGain, ValueProp.Move, null);
	}

	private void BelowMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(BelowDamage).FromMonster(this)
			.Execute(null);
	}

	private void StillDizzyMove(IReadOnlyList<Creature> targets)
	{
		return;
	}

	
}

