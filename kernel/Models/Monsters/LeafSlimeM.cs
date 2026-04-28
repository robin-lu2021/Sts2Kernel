using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.TestSupport;

namespace MegaCrit.Sts2.Core.Models.Monsters;

public sealed class LeafSlimeM : MonsterModel
{
	private const int _stickyAmount = 2;

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 33, 32);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 36, 35);

	private int ClumpDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 9, 8);

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("CLUMP_SHOT", SyncMove(ClumpShotMove), new SingleAttackIntent(ClumpDamage));
		MoveState moveState2 = new MoveState("STICKY_SHOT", SyncMove(StickyShotMove), new StatusIntent(2));
		moveState.FollowUpState = moveState2;
		moveState2.FollowUpState = moveState;
		list.Add(moveState);
		list.Add(moveState2);
		return new MonsterMoveStateMachine(list, moveState2);
	}

	private void ClumpShotMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(ClumpDamage).FromMonster(this)
			.Execute(null);
	}

	private void StickyShotMove(IReadOnlyList<Creature> targets)
	{
		CardPileCmd.AddToCombatAndPreview<Slimed>(targets, PileType.Discard, 2, addedByPlayer: false);
	}
}

