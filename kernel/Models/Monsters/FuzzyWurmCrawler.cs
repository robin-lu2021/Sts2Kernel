using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.TestSupport;

namespace MegaCrit.Sts2.Core.Models.Monsters;

public sealed class FuzzyWurmCrawler : MonsterModel
{
	private const string _inhaleTrigger = "Inhale";

	private bool _isPuffed;

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 58, 55);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 59, 57);

	private int AcidGoopDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 6, 4);

	private bool IsPuffed
	{
		get
		{
			return _isPuffed;
		}
		set
		{
			AssertMutable();
			_isPuffed = value;
		}
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("FIRST_ACID_GOOP", SyncMove(AcidGoop), new SingleAttackIntent(AcidGoopDamage));
		MoveState moveState2 = new MoveState("ACID_GOOP", SyncMove(AcidGoop), new SingleAttackIntent(AcidGoopDamage));
		MoveState moveState3 = (MoveState)(moveState.FollowUpState = new MoveState("INHALE", SyncMove(Inhale), new BuffIntent()));
		moveState3.FollowUpState = moveState2;
		moveState2.FollowUpState = moveState;
		list.Add(moveState);
		list.Add(moveState2);
		list.Add(moveState3);
		return new MonsterMoveStateMachine(list, moveState);
	}

	private void AcidGoop(IReadOnlyList<Creature> targets)
	{
		IsPuffed = false;
		DamageCmd.Attack(AcidGoopDamage).FromMonster(this)
			.Execute(null);
	}

	private void Inhale(IReadOnlyList<Creature> targets)
	{
		IsPuffed = true;
		PowerCmd.Apply<StrengthPower>(base.Creature, 7m, base.Creature, null);
	}

	
}

