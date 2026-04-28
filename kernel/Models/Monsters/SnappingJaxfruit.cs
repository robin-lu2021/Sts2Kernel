using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.TestSupport;

namespace MegaCrit.Sts2.Core.Models.Monsters;

public sealed class SnappingJaxfruit : MonsterModel
{
	private const string _chargeTrigger = "Charge";

	private bool _isCharged;

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 34, 31);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 36, 33);

	private int EnergyDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 4, 3);

	private bool IsCharged
	{
		get
		{
			return _isCharged;
		}
		set
		{
			AssertMutable();
			_isCharged = value;
		}
	}

	public override void AfterAddedToRoom()
	{
		base.AfterAddedToRoom();
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("ENERGY_ORB_MOVE", SyncMove(EnergyOrb), new SingleAttackIntent(EnergyDamage), new BuffIntent());
		moveState.FollowUpState = moveState;
		list.Add(moveState);
		return new MonsterMoveStateMachine(list, moveState);
	}

	public void EnergyOrb(IReadOnlyList<Creature> targets)
	{
		IsCharged = true;
		DamageCmd.Attack(EnergyDamage).FromMonster(this)
			.Execute(null);
		IsCharged = false;
		PowerCmd.Apply<StrengthPower>(base.Creature, 2m, base.Creature, null);
	}
}

