using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace MegaCrit.Sts2.Core.Models.Monsters;

public sealed class BowlbugRock : MonsterModel
{
	private const string _stunTrigger = "Stun";

	private const string _unstunTrigger = "Unstun";

	private bool _isOffBalance;

	private const string _spineSkin = "rock";

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 46, 45);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 49, 48);

	public static int HeadbuttDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 16, 15);

	public bool IsOffBalance
	{
		get
		{
			return _isOffBalance;
		}
		set
		{
			AssertMutable();
			_isOffBalance = value;
		}
	}

	public override void AfterAddedToRoom()
	{
		base.AfterAddedToRoom();
		PowerCmd.Apply<ImbalancedPower>(base.Creature, 1m, base.Creature, null);
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("HEADBUTT_MOVE", HeadbuttMove, new SingleAttackIntent(HeadbuttDamage));
		MoveState moveState2 = new MoveState("DIZZY_MOVE", DizzyMove, new StunIntent());
		ConditionalBranchState conditionalBranchState = (ConditionalBranchState)(moveState.FollowUpState = new ConditionalBranchState("POST_HEADBUTT"));
		moveState2.FollowUpState = moveState;
		conditionalBranchState.AddState(moveState2, () => IsOffBalance);
		conditionalBranchState.AddState(moveState, () => !IsOffBalance);
		list.Add(moveState2);
		list.Add(conditionalBranchState);
		list.Add(moveState);
		return new MonsterMoveStateMachine(list, moveState);
	}

	private void HeadbuttMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(HeadbuttDamage).FromMonster(this)
			.Execute(null);
		if (IsOffBalance)
		{
			CreatureCmd.Stun(base.Creature, DizzyMove);
		}
	}

	private void DizzyMove(IReadOnlyList<Creature> targets)
	{
		IsOffBalance = false;
	}
}

