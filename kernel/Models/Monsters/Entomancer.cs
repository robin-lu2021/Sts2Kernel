using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace MegaCrit.Sts2.Core.Models.Monsters;

public sealed class Entomancer : MonsterModel
{
	private const string _rangedAttackMove = "attack_ranged";

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 155, 145);

	public override int MaxInitialHp => MinInitialHp;

	private int SpearMoveDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 20, 18);

	private int BeesRepeat => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 8, 7);

	private int BeesDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 3, 3);

	public override void AfterAddedToRoom()
	{
		base.AfterAddedToRoom();
		PowerCmd.Apply<PersonalHivePower>(base.Creature, 1m, base.Creature, null);
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("PHEROMONE_SPIT_MOVE", SyncMove(SpitMove), new BuffIntent());
		MoveState moveState2 = new MoveState("BEES_MOVE", SyncMove(BeesMove), new MultiAttackIntent(BeesDamage, BeesRepeat));
		MoveState moveState3 = (MoveState)(moveState2.FollowUpState = new MoveState("SPEAR_MOVE", SyncMove(SpearMove), new SingleAttackIntent(SpearMoveDamage)));
		moveState3.FollowUpState = moveState;
		moveState.FollowUpState = moveState2;
		list.Add(moveState);
		list.Add(moveState2);
		list.Add(moveState3);
		return new MonsterMoveStateMachine(list, moveState2);
	}

	private void SpitMove(IReadOnlyList<Creature> targets)
	{
		PersonalHivePower personalHivePower = base.Creature.Powers.OfType<PersonalHivePower>().First();
		if (personalHivePower.Amount < 3)
		{
			PowerCmd.Apply<PersonalHivePower>(base.Creature, 1m, base.Creature, null);
			PowerCmd.Apply<StrengthPower>(base.Creature, 1m, base.Creature, null);
		}
		else
		{
			PowerCmd.Apply<StrengthPower>(base.Creature, 2m, base.Creature, null);
		}
	}

	private void BeesMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(BeesDamage).WithHitCount(BeesRepeat).FromMonster(this)
			.Execute(null);
	}

	private void SpearMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(SpearMoveDamage).FromMonster(this)
			.Execute(null);
	}

	
}

