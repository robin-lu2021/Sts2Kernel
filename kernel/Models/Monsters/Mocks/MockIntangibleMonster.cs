using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace MegaCrit.Sts2.Core.Models.Monsters.Mocks;

public sealed class MockIntangibleMonster : MonsterModel
{
	public override LocString Title => MonsterModel.L10NMonsterLookup("BIG_DUMMY.name");

	protected override string VisualsPath => SceneHelper.GetScenePath("creature_visuals/defect");

	public override int MinInitialHp => 9999;

	public override int MaxInitialHp => 9999;

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("INTANGIBLE", SyncMove(IntangibleMove), new HiddenIntent());
		MoveState moveState2 = (MoveState)(moveState.FollowUpState = new MoveState("NOTHING", SyncMove(NothingMove), new HiddenIntent()));
		moveState2.FollowUpState = moveState;
		list.Add(moveState);
		list.Add(moveState2);
		return new MonsterMoveStateMachine(list, moveState);
	}

	private void IntangibleMove(IReadOnlyList<Creature> targets)
	{
		PowerCmd.Apply<IntangiblePower>(base.Creature, 2m, base.Creature, null);
	}

	private void NothingMove(IReadOnlyList<Creature> targets)
	{
		return;
	}
}

