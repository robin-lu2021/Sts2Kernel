using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace MegaCrit.Sts2.Core.Models.Monsters;

public sealed class SpinyToad : MonsterModel
{
	private const string _spikeTrigger = "Spiked";

	private const string _unSpikeTrigger = "Unspiked";

	private bool _isSpiny;

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 121, 116);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 124, 119);

	private int LashDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 19, 17);

	private int ExplosionDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 25, 23);

	public bool IsSpiny
	{
		get
		{
			return _isSpiny;
		}
		set
		{
			AssertMutable();
			_isSpiny = value;
		}
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("PROTRUDING_SPIKES_MOVE", SyncMove(SpikesMove), new BuffIntent());
		MoveState moveState2 = new MoveState("SPIKE_EXPLOSION_MOVE", SyncMove(ExplosionMove), new SingleAttackIntent(ExplosionDamage));
		MoveState moveState3 = new MoveState("TONGUE_LASH_MOVE", SyncMove(LashMove), new SingleAttackIntent(LashDamage));
		moveState.FollowUpState = moveState2;
		moveState2.FollowUpState = moveState3;
		moveState3.FollowUpState = moveState;
		list.Add(moveState);
		list.Add(moveState2);
		list.Add(moveState3);
		return new MonsterMoveStateMachine(list, moveState);
	}

	public override void AfterAddedToRoom()
	{
		base.AfterAddedToRoom();
		return;
	}

	private void SpikesMove(IReadOnlyList<Creature> targets)
	{
		IsSpiny = true;
		PowerCmd.Apply<ThornsPower>(base.Creature, 5m, base.Creature, null);
	}

	private void ExplosionMove(IReadOnlyList<Creature> targets)
	{
		IsSpiny = false;
		DamageCmd.Attack(ExplosionDamage).FromMonster(this)
			.Execute(null);
		PowerCmd.Apply<ThornsPower>(base.Creature, -5m, base.Creature, null);
	}

	private void LashMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(LashDamage).FromMonster(this)
			.Execute(null);
	}

	
}

