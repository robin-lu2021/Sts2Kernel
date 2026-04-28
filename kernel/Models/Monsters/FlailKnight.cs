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

public class FlailKnight : MonsterModel
{
	private const string _flailAttackTrigger = "FlailAttack";

	private const string _ramAttackTrigger = "RamAttack";

	private const string _breakerAttackTrigger = "BreakerAttack";

	private const int _flailRepeat = 2;

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 108, 101);

	public override int MaxInitialHp => MinInitialHp;

	private int FlailDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 10, 9);

	private int RamDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 17, 15);

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("WAR_CHANT", SyncMove(WarChantMove), new BuffIntent());
		MoveState moveState2 = new MoveState("FLAIL_MOVE", SyncMove(FlailMove), new MultiAttackIntent(FlailDamage, 2));
		MoveState moveState3 = new MoveState("RAM_MOVE", SyncMove(RamMove), new SingleAttackIntent(RamDamage));
		RandomBranchState randomBranchState = (RandomBranchState)(moveState3.FollowUpState = (moveState2.FollowUpState = (moveState.FollowUpState = new RandomBranchState("RAND"))));
		randomBranchState.AddBranch(moveState, MoveRepeatType.CannotRepeat);
		randomBranchState.AddBranch(moveState2, 2);
		randomBranchState.AddBranch(moveState3, 2);
		list.Add(moveState);
		list.Add(moveState2);
		list.Add(moveState3);
		list.Add(randomBranchState);
		return new MonsterMoveStateMachine(list, moveState3);
	}

	private void WarChantMove(IReadOnlyList<Creature> targets)
	{
		PowerCmd.Apply<StrengthPower>(base.Creature, 3m, base.Creature, null);
	}

	public void FlailMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(FlailDamage).WithHitCount(2).FromMonster(this)
			.OnlyPlayAnimOnce()
			
			
			
			.Execute(null);
	}

	private void RamMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(RamDamage).FromMonster(this)
			
			
			.Execute(null);
	}

	
}

