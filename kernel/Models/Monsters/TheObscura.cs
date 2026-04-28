using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core.Models.Monsters;

public sealed class TheObscura : MonsterModel
{
	private const string _summonTrigger = "Summon";

	private bool _hasSummoned;

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 129, 123);

	public override int MaxInitialHp => MinInitialHp;

	private int PiercingGazeDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 11, 10);

	private int HardeningStrikeDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 7, 6);

	private int HardeningStrikeBlock => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 7, 6);

	private bool HasSummoned
	{
		get
		{
			return _hasSummoned;
		}
		set
		{
			AssertMutable();
			_hasSummoned = value;
		}
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("ILLUSION_MOVE", SyncMove(IllusionMove), new SummonIntent());
		MoveState moveState2 = new MoveState("PIERCING_GAZE_MOVE", SyncMove(PiercingGazeMove), new SingleAttackIntent(PiercingGazeDamage));
		MoveState moveState3 = new MoveState("SAIL_MOVE", SyncMove(WailMove), new BuffIntent());
		MoveState moveState4 = new MoveState("HARDENING_STRIKE_MOVE", SyncMove(HardeningStrikeMove), new SingleAttackIntent(HardeningStrikeDamage), new DefendIntent());
		RandomBranchState randomBranchState = (RandomBranchState)(moveState4.FollowUpState = (moveState3.FollowUpState = (moveState2.FollowUpState = (moveState.FollowUpState = new RandomBranchState("RAND")))));
		randomBranchState.AddBranch(moveState2, MoveRepeatType.CannotRepeat);
		randomBranchState.AddBranch(moveState3, MoveRepeatType.CannotRepeat);
		randomBranchState.AddBranch(moveState4, MoveRepeatType.CannotRepeat);
		list.Add(moveState);
		list.Add(moveState2);
		list.Add(moveState3);
		list.Add(moveState4);
		list.Add(randomBranchState);
		return new MonsterMoveStateMachine(list, moveState);
	}

	private void IllusionMove(IReadOnlyList<Creature> targets)
	{
		CreatureCmd.Add<Parafright>(base.CombatState, "illusion");
		HasSummoned = true;
	}

	private void PiercingGazeMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(PiercingGazeDamage).FromMonster(this)
			.Execute(null);
	}

	private void WailMove(IReadOnlyList<Creature> targets)
	{
		PowerCmd.Apply<StrengthPower>(base.Creature.CombatState.GetTeammatesOf(base.Creature), 3m, base.Creature, null);
	}

	private void HardeningStrikeMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(HardeningStrikeDamage).FromMonster(this)
			.Execute(null);
		CreatureCmd.GainBlock(base.Creature, HardeningStrikeBlock, ValueProp.Move, null);
	}

	
}

