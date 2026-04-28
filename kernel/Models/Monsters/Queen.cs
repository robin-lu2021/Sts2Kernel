using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core.Models.Monsters;

public sealed class Queen : MonsterModel
{
	private const string _queenTrackName = "queen_progress";

	private const int _offWithYourHeadRepeat = 5;

	private bool _hasAmalgamDied;

	private Creature? _amalgam;

	private MoveState _burnBrightForMeState;

	private MoveState _enragedState;

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 419, 400);

	public override int MaxInitialHp => MinInitialHp;

	private int OffWithYourHeadDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 4, 3);

	private int ExecutionDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 18, 15);

	private bool HasAmalgamDied
	{
		get
		{
			return _hasAmalgamDied;
		}
		set
		{
			AssertMutable();
			_hasAmalgamDied = value;
		}
	}

	private Creature? Amalgam
	{
		get
		{
			return _amalgam;
		}
		set
		{
			AssertMutable();
			_amalgam = value;
		}
	}

	private MoveState BurnBrightForMeState
	{
		get
		{
			return _burnBrightForMeState;
		}
		set
		{
			AssertMutable();
			_burnBrightForMeState = value;
		}
	}

	private MoveState EnragedState
	{
		get
		{
			return _enragedState;
		}
		set
		{
			AssertMutable();
			_enragedState = value;
		}
	}

	public override void BeforeRemovedFromRoom()
	{
		if (!base.CombatState.RunState.IsGameOver)
		{
			;
		}
	}

	public override void AfterAddedToRoom()
	{
		base.AfterAddedToRoom();
		Amalgam = base.CombatState.Enemies.First((Creature c) => c.Monster is TorchHeadAmalgam);
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("PUPPET_STRINGS_MOVE", SyncMove(PuppetStringsMove), new CardDebuffIntent());
		MoveState moveState2 = new MoveState("YOUR_MINE_MOVE", SyncMove(YoureMineMove), new DebuffIntent());
		ConditionalBranchState conditionalBranchState = new ConditionalBranchState("YOURE_MINE_NOW_BRANCH");
		BurnBrightForMeState = new MoveState("BURN_BRIGHT_FOR_ME_MOVE", SyncMove(BurnBrightForMeMove), new BuffIntent(), new DefendIntent());
		ConditionalBranchState conditionalBranchState2 = new ConditionalBranchState("BURN_BRIGHT_FOR_ME_BRANCH");
		MoveState moveState3 = new MoveState("OFF_WITH_YOUR_HEAD_MOVE", SyncMove(OffWithYourHeadMove), new MultiAttackIntent(OffWithYourHeadDamage, 5));
		MoveState moveState4 = new MoveState("EXECUTION_MOVE", SyncMove(ExecutionMove), new SingleAttackIntent(ExecutionDamage));
		EnragedState = new MoveState("ENRAGE_MOVE", SyncMove(EnrageMove), new BuffIntent());
		moveState.FollowUpState = moveState2;
		moveState2.FollowUpState = conditionalBranchState;
		conditionalBranchState.AddState(BurnBrightForMeState, () => !HasAmalgamDied);
		conditionalBranchState.AddState(moveState3, () => HasAmalgamDied);
		BurnBrightForMeState.FollowUpState = conditionalBranchState2;
		conditionalBranchState2.AddState(BurnBrightForMeState, () => !HasAmalgamDied);
		conditionalBranchState2.AddState(moveState3, () => HasAmalgamDied);
		moveState3.FollowUpState = moveState4;
		moveState4.FollowUpState = EnragedState;
		EnragedState.FollowUpState = moveState3;
		list.Add(moveState);
		list.Add(moveState2);
		list.Add(BurnBrightForMeState);
		list.Add(conditionalBranchState2);
		list.Add(conditionalBranchState);
		list.Add(moveState3);
		list.Add(moveState4);
		list.Add(EnragedState);
		return new MonsterMoveStateMachine(list, moveState);
	}

	private void PuppetStringsMove(IReadOnlyList<Creature> targets)
	{
		PowerCmd.Apply<ChainsOfBindingPower>(targets, 3m, base.Creature, null);
	}

	private void YoureMineMove(IReadOnlyList<Creature> targets)
	{
		LocString line = MonsterModel.L10NMonsterLookup("QUEEN.banter");
		PowerCmd.Apply<FrailPower>(targets, 99m, base.Creature, null);
		PowerCmd.Apply<WeakPower>(targets, 99m, base.Creature, null);
		PowerCmd.Apply<VulnerablePower>(targets, 99m, base.Creature, null);
	}

	private void BurnBrightForMeMove(IReadOnlyList<Creature> targets)
	{
		int strengthAmount = AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 1, 1);
		List<Creature> source = base.Creature.CombatState.GetTeammatesOf(base.Creature).ToList();
		foreach (Creature item in source.Where((Creature teammate) => teammate != base.Creature))
		{
			PowerCmd.Apply<StrengthPower>(item, strengthAmount, base.Creature, null);
		}
		CreatureCmd.GainBlock(base.Creature, 20m, ValueProp.Move, null);
	}

	private void OffWithYourHeadMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(OffWithYourHeadDamage).WithHitCount(5).FromMonster(this)
			.Execute(null);
	}

	private void ExecutionMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(ExecutionDamage).FromMonster(this)
			.Execute(null);
	}

	private void EnrageMove(IReadOnlyList<Creature> targets)
	{
		PowerCmd.Apply<StrengthPower>(base.Creature, 2m, base.Creature, null);
	}

	public override void AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
	{
		if (creature.Monster is TorchHeadAmalgam && base.Creature.IsAlive)
		{
			HasAmalgamDied = true;
			Amalgam = null;
			LocString line = MonsterModel.L10NMonsterLookup("QUEEN.amalgamDeathSpeakLine");
			if (base.NextMove == BurnBrightForMeState)
			{
				SetMoveImmediate(EnragedState);
			}
		}
		return;
	}
}

