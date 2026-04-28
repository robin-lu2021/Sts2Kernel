using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace MegaCrit.Sts2.Core.Models.Monsters;

public sealed class SpectralKnight : MonsterModel
{
	private const int _soulFlameRepeat = 3;

	private const string _attackFlameTrigger = "AttackFlame";

	private const string _attackSwordTrigger = "AttackSword";

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 97, 93);

	public override int MaxInitialHp => MinInitialHp;

	private int SoulSlashDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 17, 15);

	private int SoulFlameDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 4, 3);

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("HEX", SyncMove(HexMove), new DebuffIntent());
		MoveState moveState2 = new MoveState("SOUL_SLASH", SyncMove(SoulSlashMove), new SingleAttackIntent(SoulSlashDamage));
		MoveState moveState3 = new MoveState("SOUL_FLAME", SyncMove(SoulFlameMove), new MultiAttackIntent(SoulFlameDamage, 3));
		RandomBranchState randomBranchState = new RandomBranchState("RAND");
		moveState.FollowUpState = moveState2;
		moveState2.FollowUpState = randomBranchState;
		moveState3.FollowUpState = randomBranchState;
		randomBranchState.AddBranch(moveState2, 2);
		randomBranchState.AddBranch(moveState3, MoveRepeatType.CannotRepeat);
		list.Add(moveState);
		list.Add(moveState2);
		list.Add(moveState3);
		list.Add(randomBranchState);
		return new MonsterMoveStateMachine(list, moveState);
	}

	private void HexMove(IReadOnlyList<Creature> targets)
	{
		foreach (Creature target in targets)
		{
			PowerCmd.Apply<HexPower>(target, 2m, base.Creature, null);
		}
	}

	private void SoulSlashMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(SoulSlashDamage).FromMonster(this)
			.Execute(null);
	}

	private void SoulFlameMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(SoulFlameDamage).WithHitCount(3).FromMonster(this)
			.Execute(null);
	}

	
}

