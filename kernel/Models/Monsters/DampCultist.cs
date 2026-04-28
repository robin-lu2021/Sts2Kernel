using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace MegaCrit.Sts2.Core.Models.Monsters;

public sealed class DampCultist : MonsterModel
{
	private static readonly LocString _cawCawDialogue = new LocString("monsters", "DAMP_CULTIST.moves.INCANTATION.banter");

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 52, 51);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 54, 53);

	private int DarkStrikeDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 3, 1);

	private int IncantationAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 6, 5);

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("INCANTATION_MOVE", SyncMove(IncantationMove), new BuffIntent());
		MoveState moveState2 = (MoveState)(moveState.FollowUpState = new MoveState("DARK_STRIKE_MOVE", SyncMove(DarkStrikeMove), new SingleAttackIntent(DarkStrikeDamage)));
		moveState2.FollowUpState = moveState2;
		list.Add(moveState);
		list.Add(moveState2);
		return new MonsterMoveStateMachine(list, moveState);
	}

	private void IncantationMove(IReadOnlyList<Creature> targets)
	{
		PowerCmd.Apply<RitualPower>(base.Creature, IncantationAmount, base.Creature, null);
	}

	private void DarkStrikeMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(DarkStrikeDamage).FromMonster(this)
			.Execute(null);
	}
}

