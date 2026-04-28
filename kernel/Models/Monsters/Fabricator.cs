using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace MegaCrit.Sts2.Core.Models.Monsters;

public sealed class Fabricator : MonsterModel
{
	public static readonly HashSet<MonsterModel> aggroSpawns = new HashSet<MonsterModel>
	{
		KernelModelDb.Monster<Zapbot>(),
		KernelModelDb.Monster<Stabbot>()
	};

	public static readonly HashSet<MonsterModel> defenseSpawns = new HashSet<MonsterModel>
	{
		KernelModelDb.Monster<Guardbot>(),
		KernelModelDb.Monster<Noisebot>()
	};

	private MonsterModel? _lastSpawned;

	public override string HurtSfx => "event:/sfx/enemy/enemy_attacks/fabricator/fabricator_hurt";

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 155, 150);

	public override int MaxInitialHp => MinInitialHp;

	private int FabricatingStrikeDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 21, 18);

	private int DisintegrateDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 13, 11);

	public override bool ShouldFadeAfterDeath => false;

	private bool CanFabricate => base.Creature.CombatState.GetTeammatesOf(base.Creature).Count((Creature c) => c.IsAlive) < 4;

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("FABRICATE_MOVE", SyncMove(FabricateMove), new SummonIntent());
		MoveState moveState2 = new MoveState("FABRICATING_STRIKE_MOVE", SyncMove(FabricatingStrikeMove), new SingleAttackIntent(FabricatingStrikeDamage), new SummonIntent());
		MoveState moveState3 = new MoveState("DISINTEGRATE_MOVE", SyncMove(DisintegrateMove), new SingleAttackIntent(DisintegrateDamage));
		RandomBranchState randomBranchState = new RandomBranchState("RAND");
		randomBranchState.AddBranch(moveState, MoveRepeatType.CanRepeatForever, () => 1f);
		randomBranchState.AddBranch(moveState2, MoveRepeatType.CanRepeatForever, () => 1f);
		ConditionalBranchState conditionalBranchState = new ConditionalBranchState("fabricateBranch");
		conditionalBranchState.AddState(randomBranchState, () => CanFabricate);
		conditionalBranchState.AddState(moveState3, () => !CanFabricate);
		moveState.FollowUpState = conditionalBranchState;
		moveState3.FollowUpState = conditionalBranchState;
		moveState2.FollowUpState = conditionalBranchState;
		list.Add(moveState);
		list.Add(moveState2);
		list.Add(moveState3);
		list.Add(conditionalBranchState);
		list.Add(randomBranchState);
		return new MonsterMoveStateMachine(list, conditionalBranchState);
	}

	private void FabricateMove(IReadOnlyList<Creature> targets)
	{
		SpawnDefensiveBot();
		SpawnAggroBot();
	}

	private void FabricatingStrikeMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(FabricatingStrikeDamage).FromMonster(this)
			.Execute(null);
		SpawnAggroBot();
	}

	private void DisintegrateMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(DisintegrateDamage).FromMonster(this)
			.Execute(null);
	}

	private void SpawnDefensiveBot()
	{
		SpawnBot(defenseSpawns);
	}

	private void SpawnAggroBot()
	{
		SpawnBot(aggroSpawns);
	}

	private void SpawnBot(IEnumerable<MonsterModel> options)
	{
		List<MonsterModel> items = options.Where((MonsterModel m) => m != _lastSpawned).ToList();
		_lastSpawned = base.RunRng.MonsterAi.NextItem(items).ToMutable();
		Creature minion = CreatureCmd.Add(_lastSpawned, base.CombatState, CombatSide.Enemy, base.CombatState.Encounter.GetNextSlot(base.CombatState));
		PowerCmd.Apply<MinionPower>(minion, 1m, base.Creature, null);
	}
}

