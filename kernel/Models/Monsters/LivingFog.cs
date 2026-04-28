using System.Collections.Generic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
namespace MegaCrit.Sts2.Core.Models.Monsters;

public sealed class LivingFog : MonsterModel
{
	private int _bloatAmount = 1;

	private const string _spawnBombTrigger = "SpawnBomb";

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 82, 80);

	public override int MaxInitialHp => MinInitialHp;

	private int AdvancedGasDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 9, 8);

	private int BloatDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 6, 5);

	private int SuperGasBlastDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 9, 8);

	private int BloatAmount
	{
		get
		{
			return _bloatAmount;
		}
		set
		{
			AssertMutable();
			_bloatAmount = value;
		}
	}

	public override bool ShouldFadeAfterDeath => false;

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("ADVANCED_GAS_MOVE", SyncMove(AdvancedGasMove), new SingleAttackIntent(AdvancedGasDamage), new CardDebuffIntent());
		MoveState moveState2 = new MoveState("BLOAT_MOVE", SyncMove(BloatMove), new SingleAttackIntent(BloatDamage), new SummonIntent());
		MoveState moveState3 = new MoveState("SUPER_GAS_BLAST_MOVE", SyncMove(SuperGasBlastMove), new SingleAttackIntent(SuperGasBlastDamage));
		moveState.FollowUpState = moveState2;
		moveState2.FollowUpState = moveState3;
		moveState3.FollowUpState = moveState2;
		list.Add(moveState);
		list.Add(moveState3);
		list.Add(moveState2);
		return new MonsterMoveStateMachine(list, moveState);
	}

	private void AdvancedGasMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(AdvancedGasDamage).FromMonster(this)
			.Execute(null);
		PowerCmd.Apply<SmoggyPower>(targets, 1m, base.Creature, null);
	}

	private void BloatMove(IReadOnlyList<Creature> targets)
	{
		for (int i = 0; i < BloatAmount; i++)
		{
			string nextSlot = base.CombatState.Encounter.GetNextSlot(base.CombatState);
			if (nextSlot != "")
			{
				CreatureCmd.Add<GasBomb>(base.CombatState, nextSlot);
			}
		}
		DamageCmd.Attack(BloatDamage).FromMonster(this)
			.Execute(null);
	}

	private void SuperGasBlastMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(SuperGasBlastDamage).FromMonster(this)
			.Execute(null);
	}

	
}

