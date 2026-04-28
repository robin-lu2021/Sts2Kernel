using System.Collections.Generic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
namespace MegaCrit.Sts2.Core.Models.Monsters;

public sealed class GasBomb : MonsterModel
{
	private const string _explodeTrigger = "ExplodeTrigger";

	private bool _hasExploded;

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 8, 7);

	public override int MaxInitialHp => MinInitialHp;

	private int ExplodeDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 9, 8);

	private bool HasExploded
	{
		get
		{
			return _hasExploded;
		}
		set
		{
			AssertMutable();
			_hasExploded = value;
		}
	}

	public override bool ShouldFadeAfterDeath => false;

	public override void AfterAddedToRoom()
	{
		base.AfterAddedToRoom();
		PowerCmd.Apply<MinionPower>(base.Creature, 1m, base.Creature, null);
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("EXPLODE_MOVE", SyncMove(ExplodeMove), new DeathBlowIntent(() => ExplodeDamage));
		list.Add(moveState);
		return new MonsterMoveStateMachine(list, moveState);
	}

	private void ExplodeMove(IReadOnlyList<Creature> targets)
	{
		HasExploded = true;
		DamageCmd.Attack(ExplodeDamage).FromMonster(this)
			.Execute(null);
		CreatureCmd.Kill(base.Creature);
	}

	
}

