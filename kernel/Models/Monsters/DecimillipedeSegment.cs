using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.TestSupport;

namespace MegaCrit.Sts2.Core.Models.Monsters;

public abstract class DecimillipedeSegment : MonsterModel
{
	private int _starterMoveIdx;

	private const int _writheRepeat = 2;

	private MoveState _deadState;

	public override LocString Title => MonsterModel.L10NMonsterLookup("DECIMILLIPEDE_SEGMENT.name");

	public int StarterMoveIdx
	{
		get
		{
			return _starterMoveIdx;
		}
		set
		{
			AssertMutable();
			_starterMoveIdx = value;
		}
	}

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 46, 40);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 52, 46);

	private int WritheDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 6, 5);

	private int ConstrictDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 9, 8);

	private int BulkDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 7, 6);

	private int BulkStrength => 2;

	public override float HpBarSizeReduction => 35f;

	public MoveState DeadState
	{
		get
		{
			return _deadState;
		}
		private set
		{
			AssertMutable();
			_deadState = value;
		}
	}

	public override bool ShouldFadeAfterDeath => false;

	public override bool ShouldDisappearFromDoom => false;

	public override bool CanChangeScale => false;

	public override void AfterAddedToRoom()
	{
		base.AfterAddedToRoom();
		decimal maxHp = base.Creature.MaxHp;
		if (maxHp % 2m == 1m)
		{
			maxHp++;
		}
		IReadOnlyList<Player> players = base.CombatState.Players;
		int count = players.Count;
		int currentActIndex = base.CombatState.RunState.CurrentActIndex;
		List<Creature> source = (from c in base.CombatState.GetTeammatesOf(base.Creature)
			where c != base.Creature
			select c).ToList();
		while (source.Any((Creature c) => (decimal)c.MaxHp == maxHp))
		{
			maxHp += 2m;
			if (maxHp > MegaCrit.Sts2.Core.Entities.Creatures.Creature.ScaleHpForMultiplayer(MaxInitialHp, base.CombatState.Encounter, count, currentActIndex))
			{
				maxHp = MegaCrit.Sts2.Core.Entities.Creatures.Creature.ScaleHpForMultiplayer(MinInitialHp, base.CombatState.Encounter, count, currentActIndex);
			}
		}
		CreatureCmd.SetMaxAndCurrentHp(base.Creature, maxHp);
		PowerCmd.Apply<ReattachPower>(base.Creature, 25m, base.Creature, null);
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("WRITHE_MOVE", SyncMove(WritheMove), new MultiAttackIntent(WritheDamage, 2));
		MoveState moveState2 = new MoveState("BULK_MOVE", SyncMove(BulkMove), new SingleAttackIntent(BulkDamage), new BuffIntent());
		MoveState moveState3 = new MoveState("CONSTRICT_MOVE", SyncMove(ConstrictMove), new SingleAttackIntent(ConstrictDamage), new DebuffIntent());
		DeadState = new MoveState("DEAD_MOVE", SyncMove(DeadMove));
		MoveState moveState4 = new MoveState("REATTACH_MOVE", SyncMove(ReattachMove), new HealIntent());
		moveState3.FollowUpState = moveState2;
		moveState2.FollowUpState = moveState;
		moveState.FollowUpState = moveState3;
		RandomBranchState randomBranchState = new RandomBranchState("RAND");
		DeadState.FollowUpState = moveState4;
		moveState4.FollowUpState = randomBranchState;
		randomBranchState.AddBranch(moveState, MoveRepeatType.CannotRepeat);
		randomBranchState.AddBranch(moveState2, MoveRepeatType.CannotRepeat);
		randomBranchState.AddBranch(moveState3, MoveRepeatType.CannotRepeat);
		list.Add(moveState);
		list.Add(moveState2);
		list.Add(moveState3);
		list.Add(DeadState);
		list.Add(moveState4);
		list.Add(randomBranchState);
		return new MonsterMoveStateMachine(list, (StarterMoveIdx % 3) switch
		{
			0 => moveState, 
			1 => moveState2, 
			_ => moveState3, 
		});
	}

	private void WritheMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(WritheDamage).WithHitCount(2).FromMonster(this)
			.Execute(null);
	}

	private void BulkMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(BulkDamage).FromMonster(this)
			.Execute(null);
		PowerCmd.Apply<StrengthPower>(base.Creature, BulkStrength, base.Creature, null);
	}

	private void ConstrictMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(ConstrictDamage).FromMonster(this)
			.Execute(null);
		PowerCmd.Apply<WeakPower>(targets, 1m, base.Creature, null);
	}

	private void DeadMove(IReadOnlyList<Creature> targets)
	{
		return;
	}

	private void ReattachMove(IReadOnlyList<Creature> targets)
	{
		base.Creature.GetPower<ReattachPower>().DoReattach();
	}
	
	protected abstract void SegmentAttack();
}

