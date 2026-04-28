using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace MegaCrit.Sts2.Core.Models.Monsters;

public sealed class TheLost : MonsterModel
{
	private const int _eyeLasersRepeat = 2;

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 99, 93);

	public override int MaxInitialHp => MinInitialHp;

	private int EyeLasersDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 5, 4);

	private int DebilitatingSmogStrengthStealAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 2, 2);

	public override void AfterAddedToRoom()
	{
		PowerCmd.Apply<PossessStrengthPower>(base.Creature, 1m, null, null);
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("DEBILITATING_SMOG", SyncMove(DebilitatingSmogMove), new DebuffIntent(), new BuffIntent());
		MoveState moveState2 = (MoveState)(moveState.FollowUpState = new MoveState("EYE_LASERS", SyncMove(EyeLasersMove), new MultiAttackIntent(EyeLasersDamage, 2)));
		moveState2.FollowUpState = moveState;
		list.Add(moveState);
		list.Add(moveState2);
		return new MonsterMoveStateMachine(list, moveState);
	}

	private void DebilitatingSmogMove(IReadOnlyList<Creature> targets)
	{
		PowerCmd.Apply<StrengthPower>(targets, -DebilitatingSmogStrengthStealAmount, base.Creature, null);
		PowerCmd.Apply<StrengthPower>(base.Creature, DebilitatingSmogStrengthStealAmount, base.Creature, null);
	}

	private void EyeLasersMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(EyeLasersDamage).WithHitCount(2).FromMonster(this)
			.Execute(null);
	}

	
}

