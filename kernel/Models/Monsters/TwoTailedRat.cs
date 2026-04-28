using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Random;

namespace MegaCrit.Sts2.Core.Models.Monsters;

public sealed class TwoTailedRat : MonsterModel
{
	private static readonly string[] _barnacleOptions = new string[3] { "barnacle1", "barnacle1", "barnacle3" };

	private static readonly string[] _headOptions = new string[3] { "head1", "head2", "head3" };

	private const string _callForBackupMoveId = "CALL_FOR_BACKUP_MOVE";

	private const float _callForBackupChance = 0.75f;

	private const int _callForBackupLimit = 3;

	private int _starterMoveIndex = -1;

	private int _turnsUntilSummonable = 2;

	private int _callForBackupCount;

	private const string _summonTrigger = "Summon";

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 18, 17);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 22, 21);

	private int ScratchDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 9, 8);

	private int DiseaseBiteDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 7, 6);

	public int StarterMoveIndex
	{
		get
		{
			return _starterMoveIndex;
		}
		set
		{
			AssertMutable();
			_starterMoveIndex = value;
		}
	}

	private int TurnsUntilSummonable
	{
		get
		{
			return _turnsUntilSummonable;
		}
		set
		{
			AssertMutable();
			_turnsUntilSummonable = value;
		}
	}

	public int CallForBackupCount
	{
		get
		{
			return _callForBackupCount;
		}
		set
		{
			AssertMutable();
			_callForBackupCount = value;
		}
	}

	public override void AfterAddedToRoom()
	{
		base.AfterAddedToRoom();
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("SCRATCH_MOVE", SyncMove(ScratchMove), new SingleAttackIntent(ScratchDamage));
		MoveState moveState2 = new MoveState("DISEASE_BITE_MOVE", SyncMove(DiseaseBiteMove), new SingleAttackIntent(DiseaseBiteDamage));
		MoveState moveState3 = new MoveState("SCREECH_MOVE", SyncMove(ScreechMove), new DebuffIntent());
		MoveState moveState4 = new MoveState("CALL_FOR_BACKUP_MOVE", SyncMove(CallForBackup), new SummonIntent());
		RandomBranchState randomBranchState = (RandomBranchState)(moveState4.FollowUpState = (moveState3.FollowUpState = (moveState2.FollowUpState = (moveState.FollowUpState = new RandomBranchState("RAND")))));
		randomBranchState.AddBranch(moveState, MoveRepeatType.CannotRepeat, () => (!CanSummon() ? 1f : (1f / 12f)));
		randomBranchState.AddBranch(moveState2, MoveRepeatType.CannotRepeat, () => (!CanSummon() ? 1f : (1f / 12f)));
		randomBranchState.AddBranch(moveState3, 3, MoveRepeatType.CannotRepeat, () => (!CanSummon() ? 1f : (1f / 12f)));
		randomBranchState.AddBranch(moveState4, MoveRepeatType.UseOnlyOnce, () => (!CanSummon() ? 0f : 0.75f));
		list.Add(randomBranchState);
		list.Add(moveState);
		list.Add(moveState2);
		list.Add(moveState3);
		list.Add(moveState4);
		if (StarterMoveIndex == -1)
		{
			return new MonsterMoveStateMachine(list, randomBranchState);
		}
		return new MonsterMoveStateMachine(list, (StarterMoveIndex % 3) switch
		{
			0 => moveState, 
			1 => moveState2, 
			_ => moveState3, 
		});
	}

	private void ScratchMove(IReadOnlyList<Creature> targets)
	{
		TurnsUntilSummonable--;
		DamageCmd.Attack(ScratchDamage).FromMonster(this)
			.Execute(null);
	}

	private void DiseaseBiteMove(IReadOnlyList<Creature> targets)
	{
		TurnsUntilSummonable--;
		DamageCmd.Attack(DiseaseBiteDamage).FromMonster(this)
			.Execute(null);
	}

	private void ScreechMove(IReadOnlyList<Creature> targets)
	{
		TurnsUntilSummonable--;
		PowerCmd.Apply<FrailPower>(targets, 1m, base.Creature, null);
	}

	private void CallForBackup(IReadOnlyList<Creature> targets)
	{
		string nextSlot = base.CombatState.Encounter.Slots.LastOrDefault((string s) => base.CombatState.Enemies.All((Creature c) => c.SlotName != s), string.Empty);
		if (!string.IsNullOrEmpty(nextSlot))
		{
			CreatureCmd.Add<TwoTailedRat>(base.CombatState, nextSlot);
		}
		List<TwoTailedRat> list = base.Creature.CombatState.Enemies.Select((Creature c) => c.Monster).OfType<TwoTailedRat>().ToList();
		int maxCallForBackupCount = list.Max((TwoTailedRat c) => c.CallForBackupCount + 1);
		list.ForEach(delegate(TwoTailedRat r)
		{
			r.CallForBackupCount = maxCallForBackupCount;
		});
	}

	private bool CanSummon()
	{
		if (TurnsUntilSummonable > 0)
		{
			return false;
		}
		if (CallForBackupCount >= 3)
		{
			return false;
		}
		if (string.IsNullOrEmpty(base.CombatState.Encounter?.GetNextSlot(base.CombatState)))
		{
			return false;
		}
		List<Creature> list = (from c in base.Creature.CombatState.GetTeammatesOf(base.Creature)
			where c != base.Creature
			select c).ToList();
		foreach (Creature item in list)
		{
			if (item.Monster.NextMove.Id.Equals("CALL_FOR_BACKUP_MOVE"))
			{
				return false;
			}
		}
		return true;
	}

	
}

