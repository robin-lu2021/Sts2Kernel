using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace MegaCrit.Sts2.Core.Models.Monsters;

public sealed class Osty : MonsterModel
{
	public const float attackerAnimDelay = 0.3f;

	public const string pokeAnim = "attack_poke";

	public override int MinInitialHp => 1;

	public override int MaxInitialHp => 1;

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		MoveState moveState = new MoveState("NOTHING_MOVE", SyncMove((IReadOnlyList<Creature> _) => { }));
		moveState.FollowUpState = moveState;
		return new MonsterMoveStateMachine(new global::_003C_003Ez__ReadOnlySingleElementList<MonsterState>(moveState), moveState);
	}
	
	public static bool CheckMissingWithAnim(Player owner)
	{
		return owner.IsOstyMissing;
	}
}
