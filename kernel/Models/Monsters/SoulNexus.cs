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

public sealed class SoulNexus : MonsterModel
{
	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 254, 234);

	public override int MaxInitialHp => MinInitialHp;

	private int SoulBurnDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 31, 29);

	private int MaelstromDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 7, 6);

	private int MaelstromRepeat => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 4, 4);

	private int DrainLifeDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 19, 18);

	public override bool ShouldFadeAfterDeath => false;

	public override void AfterAddedToRoom()
	{
		base.AfterAddedToRoom();
	}

	public override void BeforeRemovedFromRoom()
	{
		if (!base.CombatState.RunState.IsGameOver)
		{
			;
		}
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("SOUL_BURN_MOVE", SyncMove(SoulBurnMove), new SingleAttackIntent(SoulBurnDamage));
		MoveState moveState2 = new MoveState("MAELSTROM_MOVE", SyncMove(MaelstromMove), new MultiAttackIntent(MaelstromDamage, MaelstromRepeat));
		MoveState moveState3 = new MoveState("DRAIN_LIFE_MOVE", SyncMove(DrainLifeMove), new SingleAttackIntent(DrainLifeDamage), new DebuffIntent(strong: true));
		RandomBranchState randomBranchState = (RandomBranchState)(moveState3.FollowUpState = (moveState2.FollowUpState = (moveState.FollowUpState = new RandomBranchState("RAND"))));
		randomBranchState.AddBranch(moveState, MoveRepeatType.CannotRepeat, 1f);
		randomBranchState.AddBranch(moveState2, MoveRepeatType.CannotRepeat, 1f);
		randomBranchState.AddBranch(moveState3, MoveRepeatType.CannotRepeat, 1f);
		list.Add(moveState);
		list.Add(moveState2);
		list.Add(moveState3);
		list.Add(randomBranchState);
		return new MonsterMoveStateMachine(list, moveState);
	}

	private void SoulBurnMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(SoulBurnDamage).FromMonster(this)
			.Execute(null);
	}

	private void MaelstromMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(MaelstromDamage).WithHitCount(MaelstromRepeat).FromMonster(this)
			.Execute(null);
	}

	private void DrainLifeMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(DrainLifeDamage).FromMonster(this)
			.Execute(null);
		PowerCmd.Apply<VulnerablePower>(targets, 2m, base.Creature, null);
		PowerCmd.Apply<WeakPower>(targets, 2m, base.Creature, null);
	}
}

