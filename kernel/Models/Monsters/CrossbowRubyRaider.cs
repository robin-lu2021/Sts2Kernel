using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core.Models.Monsters;

public sealed class CrossbowRubyRaider : MonsterModel
{
	private const string _reloadTrigger = "Reload";

	private bool _isCrossbowReloaded;

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 19, 18);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 22, 21);

	private int FireDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 16, 14);

	private bool IsCrossbowReloaded
	{
		get
		{
			return _isCrossbowReloaded;
		}
		set
		{
			AssertMutable();
			_isCrossbowReloaded = value;
		}
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("FIRE_MOVE", SyncMove(FireMove), new SingleAttackIntent(FireDamage));
		MoveState moveState2 = (MoveState)(moveState.FollowUpState = new MoveState("RELOAD_MOVE", SyncMove(ReloadMove), new DefendIntent()));
		moveState2.FollowUpState = moveState;
		list.Add(moveState2);
		list.Add(moveState);
		return new MonsterMoveStateMachine(list, moveState2);
	}

	private void FireMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(FireDamage).FromMonster(this)
			.Execute(null);
		IsCrossbowReloaded = false;
	}

	private void ReloadMove(IReadOnlyList<Creature> targets)
	{
		CreatureCmd.GainBlock(base.Creature, 3m, ValueProp.Move, null);
		IsCrossbowReloaded = true;
	}

	
}

