using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace MegaCrit.Sts2.Core.Models.Monsters;

public sealed class SlimedBerserker : MonsterModel
{
	private const int _pummelingRepeat = 4;

	private const int _leechingDrain = 3;

	private const int _vomitSlimeInDiscard = 10;

	private const string _hugTrigger = "Hug";

	private const string _vomitTrigger = "Vomit";

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 276, 266);

	public override int MaxInitialHp => MinInitialHp;

	private int PummelingDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 5, 4);

	private int SmotherDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 33, 30);

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("VOMIT_ICHOR_MOVE", SyncMove(VomitIchorMove), new StatusIntent(10));
		MoveState moveState2 = new MoveState("LEECHING_HUG_MOVE", SyncMove(LeechingHugMove), new DebuffIntent(), new BuffIntent());
		MoveState moveState3 = new MoveState("SMOTHER_MOVE", SyncMove(SmotherMove), new SingleAttackIntent(SmotherDamage));
		MoveState moveState4 = (MoveState)(moveState.FollowUpState = new MoveState("FURIOUS_PUMMELING_MOVE", SyncMove(FuriousPummelingMove), new MultiAttackIntent(PummelingDamage, 4)));
		moveState4.FollowUpState = moveState2;
		moveState2.FollowUpState = moveState3;
		moveState3.FollowUpState = moveState;
		list.Add(moveState);
		list.Add(moveState3);
		list.Add(moveState2);
		list.Add(moveState4);
		return new MonsterMoveStateMachine(list, moveState);
	}

	private void VomitIchorMove(IReadOnlyList<Creature> targets)
	{
		CardPileCmd.AddToCombatAndPreview<Slimed>(targets, PileType.Discard, 10, addedByPlayer: false);
	}

	private void LeechingHugMove(IReadOnlyList<Creature> targets)
	{
		PowerCmd.Apply<WeakPower>(targets, 3m, null, null);
		PowerCmd.Apply<StrengthPower>(base.Creature, 3m, base.Creature, null);
	}

	private void FuriousPummelingMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(PummelingDamage).WithHitCount(4)
			.FromMonster(this)
			.Execute(null);
	}

	private void SmotherMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(SmotherDamage).FromMonster(this)
			.Execute(null);
	}

	
}

