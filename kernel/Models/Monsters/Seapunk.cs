using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core.Models.Monsters;

public sealed class Seapunk : MonsterModel
{
	private const string _multiAttackTrigger = "MultiAttack";

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 47, 44);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 49, 46);

	private int SeaKickDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 13, 11);

	private int SpinningKickDamage => 2;

	private int SpinningKickRepeat => 4;

	private int BubbleBlock => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 8, 7);

	private int BubbleStr => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 2, 1);

	public override void AfterAddedToRoom()
	{
		base.AfterAddedToRoom();
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("SEA_KICK_MOVE", SyncMove(SeaKickMove), new SingleAttackIntent(SeaKickDamage));
		MoveState moveState2 = new MoveState("SPINNING_KICK_MOVE", SyncMove(SpinningKickMove), new MultiAttackIntent(SpinningKickDamage, SpinningKickRepeat));
		MoveState moveState3 = new MoveState("BUBBLE_BURP_MOVE", SyncMove(BubbleBurpMove), new BuffIntent(), new DefendIntent());
		moveState.FollowUpState = moveState2;
		moveState2.FollowUpState = moveState3;
		moveState3.FollowUpState = moveState;
		list.Add(moveState);
		list.Add(moveState2);
		list.Add(moveState3);
		return new MonsterMoveStateMachine(list, moveState);
	}

	private void SeaKickMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(SeaKickDamage).FromMonster(this)
			.Execute(null);
	}

	private void SpinningKickMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(SpinningKickDamage).WithHitCount(SpinningKickRepeat).FromMonster(this)
			.Execute(null);
	}

	private void BubbleBurpMove(IReadOnlyList<Creature> targets)
	{
		CreatureCmd.GainBlock(base.Creature, BubbleBlock, ValueProp.Move, null);
		PowerCmd.Apply<StrengthPower>(base.Creature, BubbleStr, base.Creature, null);
	}

	
}

