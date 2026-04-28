using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace MegaCrit.Sts2.Core.Models.Monsters;

public sealed class FossilStalker : MonsterModel
{
	private const string _attackDoubleTrigger = "AttackDouble";

	private const string _attackBuff = "event:/sfx/enemy/enemy_attacks/fossil_stalker/fossil_stalker_attack_buff";

	private const string _attackDouble = "event:/sfx/enemy/enemy_attacks/fossil_stalker/fossil_stalker_attack_double";

	private const string _attackSingle = "event:/sfx/enemy/enemy_attacks/fossil_stalker/fossil_stalker_attack_single";

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 54, 51);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 56, 53);

	private int TackleDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 11, 9);

	private int LatchDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 14, 12);

	private int LashDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 4, 3);

	private int LashRepeat => 2;

	public override void AfterAddedToRoom()
	{
		base.AfterAddedToRoom();
		PowerCmd.Apply<SuckPower>(base.Creature, 3m, base.Creature, null);
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("TACKLE_MOVE", SyncMove(TackleMove), new SingleAttackIntent(TackleDamage), new DebuffIntent());
		MoveState moveState2 = new MoveState("LATCH_MOVE", SyncMove(LatchMove), new SingleAttackIntent(LatchDamage));
		MoveState moveState3 = new MoveState("LASH_MOVE", SyncMove(LashAttack), new MultiAttackIntent(LashDamage, LashRepeat));
		RandomBranchState randomBranchState = (RandomBranchState)(moveState3.FollowUpState = (moveState.FollowUpState = (moveState2.FollowUpState = new RandomBranchState("RAND"))));
		randomBranchState.AddBranch(moveState2, 2);
		randomBranchState.AddBranch(moveState, 2);
		randomBranchState.AddBranch(moveState3, 2);
		list.Add(randomBranchState);
		list.Add(moveState);
		list.Add(moveState2);
		list.Add(moveState3);
		return new MonsterMoveStateMachine(list, moveState2);
	}

	private void TackleMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(TackleDamage).FromMonster(this)
			
			
			.Execute(null);
		PowerCmd.Apply<FrailPower>(targets, 1m, base.Creature, null);
	}

	private void LatchMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(LatchDamage).FromMonster(this)
			
			
			.Execute(null);
	}

	private void LashAttack(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(LashDamage).WithHitCount(LashRepeat).FromMonster(this)
			
			.OnlyPlayAnimOnce()
			
			
			.Execute(null);
	}

	
}

