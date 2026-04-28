using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core.Models.Monsters;

public sealed class LagavulinMatriarch : MonsterModel
{
	public const string slashMoveId = "SLASH_MOVE";

	private const string _sleepTrigger = "Sleep";

	public const string wakeTrigger = "Wake";

	private const string _attackHeavyTrigger = "AttackHeavy";

	private const string _attackDoubleTrigger = "AttackDouble";

	private bool _isAwake;

	private bool _isShellAwake;

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 233, 222);

	public override int MaxInitialHp => MinInitialHp;

	private int SlashDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 21, 19);

	private int Slash2Damage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 14, 12);

	private int Slash2Block => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 14, 12);

	private int DisembowelDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 10, 9);

	private int DisembowelRepeat => 2;

	public bool IsAwake
	{
		get
		{
			return _isAwake;
		}
		set
		{
			AssertMutable();
			_isAwake = value;
		}
	}

	public bool IsShellAwake
	{
		get
		{
			return _isShellAwake;
		}
		set
		{
			AssertMutable();
			_isShellAwake = value;
		}
	}

	public override void AfterAddedToRoom()
	{
		base.AfterAddedToRoom();
		PowerCmd.Apply<PlatingPower>(base.Creature, 12m, base.Creature, null);
		PowerCmd.Apply<AsleepPower>(base.Creature, 3m, base.Creature, null);
	}

	public override void AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		if (target != base.Creature)
		{
			return;
		}
		if (base.Creature.CurrentHp <= base.Creature.MaxHp / 2 && !IsShellAwake)
		{
			IsShellAwake = true;
		}
		return;
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("SLEEP_MOVE", SyncMove(SleepMove), new SleepIntent());
		MoveState moveState2 = new MoveState("SLASH_MOVE", SyncMove(SlashMove), new SingleAttackIntent(SlashDamage));
		MoveState moveState3 = new MoveState("SLASH2_MOVE", SyncMove(Slash2Move), new SingleAttackIntent(Slash2Damage), new DefendIntent());
		MoveState moveState4 = new MoveState("DISEMBOWEL_MOVE", SyncMove(DisembowelMove), new MultiAttackIntent(DisembowelDamage, DisembowelRepeat));
		MoveState moveState5 = new MoveState("SOUL_SIPHON_MOVE", SyncMove(SoulSiphonMove), new DebuffIntent(), new BuffIntent());
		ConditionalBranchState conditionalBranchState = (ConditionalBranchState)(moveState.FollowUpState = new ConditionalBranchState("SLEEP_BRANCH"));
		moveState2.FollowUpState = moveState4;
		moveState4.FollowUpState = moveState3;
		moveState3.FollowUpState = moveState5;
		moveState5.FollowUpState = moveState2;
		conditionalBranchState.AddState(moveState, () => base.Creature.HasPower<AsleepPower>());
		conditionalBranchState.AddState(moveState2, () => !base.Creature.HasPower<AsleepPower>());
		list.Add(conditionalBranchState);
		list.Add(moveState);
		list.Add(moveState2);
		list.Add(moveState3);
		list.Add(moveState5);
		list.Add(moveState4);
		return new MonsterMoveStateMachine(list, moveState);
	}

	private void SleepMove(IReadOnlyList<Creature> targets)
	{
		return;
	}

	public void WakeUpMove(IReadOnlyList<Creature> _)
	{
		if (!_isAwake)
		{
			IsAwake = true;
		}
	}

	private void SlashMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(SlashDamage).FromMonster(this)
			.Execute(null);
	}

	private void Slash2Move(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(Slash2Damage).FromMonster(this)
			.Execute(null);
		CreatureCmd.GainBlock(base.Creature, Slash2Block, ValueProp.Move, null);
	}

	private void DisembowelMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(DisembowelDamage).WithHitCount(DisembowelRepeat).FromMonster(this)
			.Execute(null);
	}

	private void SoulSiphonMove(IReadOnlyList<Creature> targets)
	{
		PowerCmd.Apply<StrengthPower>(targets, -2m, base.Creature, null);
		PowerCmd.Apply<DexterityPower>(targets, -2m, base.Creature, null);
		PowerCmd.Apply<StrengthPower>(base.Creature, 2m, base.Creature, null);
	}
}

