using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace MegaCrit.Sts2.Core.Models.Monsters;

public sealed class GremlinMerc : MonsterModel
{
	private bool _hasSpoken;

	private const string _attackDoubleTrigger = "AttackDouble";

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 51, 47);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 53, 49);

	private int GimmeDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 8, 7);

	private int GimmeRepeat => 2;

	private int DoubleSmashDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 7, 6);

	private int DoubleSmashRepeat => 2;

	public int HeheDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 9, 8);

	public override void AfterAddedToRoom()
	{
		base.AfterAddedToRoom();
		PowerCmd.Apply<SurprisePower>(base.Creature, 1m, base.Creature, null);
		foreach (Player player in base.Creature.CombatState.Players)
		{
			ThieveryPower thieveryPower = (ThieveryPower)KernelModelDb.Power<ThieveryPower>().ToMutable();
			thieveryPower.Target = player.Creature;
			PowerCmd.Apply(thieveryPower, base.Creature, 20m, base.Creature, null);
		}
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("GIMME_MOVE", SyncMove(GimmeMove), new MultiAttackIntent(GimmeDamage, GimmeRepeat));
		MoveState moveState2 = new MoveState("DOUBLE_SMASH_MOVE", SyncMove(DoubleSmashMove), new MultiAttackIntent(DoubleSmashDamage, DoubleSmashRepeat), new DebuffIntent());
		MoveState moveState3 = new MoveState("HEHE_MOVE", SyncMove(HeheMove), new SingleAttackIntent(HeheDamage), new BuffIntent());
		moveState.FollowUpState = moveState2;
		moveState2.FollowUpState = moveState3;
		moveState3.FollowUpState = moveState;
		list.Add(moveState);
		list.Add(moveState2);
		list.Add(moveState3);
		return new MonsterMoveStateMachine(list, moveState);
	}

	private void GimmeMove(IReadOnlyList<Creature> targets)
	{
		if (!_hasSpoken)
		{
			_hasSpoken = true;
			LocString line = MonsterModel.L10NMonsterLookup("GREMLIN_MERC.moves.GIMME.banter");
		}
		DamageCmd.Attack(GimmeDamage).WithHitCount(GimmeRepeat).FromMonster(this)
			.Execute(null);
		foreach (ThieveryPower powerInstance in base.Creature.GetPowerInstances<ThieveryPower>())
		{
			powerInstance.Steal();
		}
	}

	private void DoubleSmashMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(DoubleSmashDamage).WithHitCount(DoubleSmashRepeat).FromMonster(this)
			.Execute(null);
		foreach (ThieveryPower powerInstance in base.Creature.GetPowerInstances<ThieveryPower>())
		{
			powerInstance.Steal();
		}
		PowerCmd.Apply<WeakPower>(targets, 2m, base.Creature, null);
	}

	private void HeheMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(HeheDamage).FromMonster(this)
			.Execute(null);
		foreach (ThieveryPower powerInstance in base.Creature.GetPowerInstances<ThieveryPower>())
		{
			powerInstance.Steal();
		}
		PowerCmd.Apply<StrengthPower>(base.Creature, 2m, base.Creature, null);
	}

	
}

