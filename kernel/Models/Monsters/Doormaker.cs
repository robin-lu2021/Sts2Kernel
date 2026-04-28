using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace MegaCrit.Sts2.Core.Models.Monsters;

public sealed class Doormaker : MonsterModel
{
	private const string _doormakerTrackName = "queen_progress";

	private const string _dramaticOpenLine = "DOORMAKER.moves.DRAMATIC_OPEN.speakLine";

	private const int _graspHitCount = 2;

	private int _originalHp;

	private bool _isPortalOpen;

	public override LocString Title
	{
		get
		{
			if (!IsPortalOpen)
			{
				return MonsterModel.L10NMonsterLookup("DOOR.name");
			}
			return MonsterModel.L10NMonsterLookup(base.Id.Entry + ".name");
		}
	}
	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 512, 489);

	public override int MaxInitialHp => MinInitialHp;

	private int HungerDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 35, 30);

	private int ScrutinyDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 26, 24);

	private int GraspDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 11, 10);

	private int GraspStrengthGain => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 4, 3);

	private int OriginalHp
	{
		get
		{
			return _originalHp;
		}
		set
		{
			AssertMutable();
			_originalHp = value;
		}
	}

	private bool IsPortalOpen
	{
		get
		{
			return _isPortalOpen;
		}
		set
		{
			AssertMutable();
			_isPortalOpen = value;
		}
	}

	public override void AfterAddedToRoom()
	{
		base.AfterAddedToRoom();
		OriginalHp = base.Creature.MaxHp;
		CreatureCmd.SetMaxAndCurrentHp(base.Creature, 999999999m);
		base.Creature.ShowsInfiniteHp = true;
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("DRAMATIC_OPEN_MOVE", DramaticOpenMove, new SummonIntent());
		MoveState moveState2 = new MoveState("HUNGER_MOVE", HungerMove, new SingleAttackIntent(HungerDamage));
		MoveState moveState3 = new MoveState("SCRUTINY_MOVE", ScrutinyMove, new SingleAttackIntent(ScrutinyDamage));
		MoveState moveState4 = new MoveState("GRASP_MOVE", GraspMove, new MultiAttackIntent(GraspDamage, 2), new BuffIntent());
		moveState.FollowUpState = moveState2;
		moveState2.FollowUpState = moveState3;
		moveState3.FollowUpState = moveState4;
		moveState4.FollowUpState = moveState2;
		list.Add(moveState);
		list.Add(moveState2);
		list.Add(moveState3);
		list.Add(moveState4);
		return new MonsterMoveStateMachine(list, moveState);
	}

	private void DramaticOpenMove(IReadOnlyList<Creature> targets)
	{
		IsPortalOpen = true;
		CreatureCmd.SetMaxAndCurrentHp(base.Creature, OriginalHp);
		List<PowerModel> list = base.Creature.Powers.ToList();
		foreach (PowerModel item in list)
		{
			PowerCmd.Remove(item);
		}
		base.Creature.ShowsInfiniteHp = false;
		SwapPhasePower<HungerPower>();
	}

	private void HungerMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(HungerDamage).FromMonster(this)
			.Execute(null);
		SwapPhasePower<ScrutinyPower>();
	}

	private void ScrutinyMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(ScrutinyDamage).FromMonster(this)
			.Execute(null);
		SwapPhasePower<GraspPower>();
	}

	private void GraspMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(GraspDamage).WithHitCount(2).FromMonster(this)
			.Execute(null);
		PowerCmd.Apply<StrengthPower>(base.Creature, GraspStrengthGain, base.Creature, null);
		SwapPhasePower<HungerPower>();
	}

	private void SwapPhasePower<T>() where T : PowerModel
	{
		if (base.Creature.HasPower<HungerPower>())
		{
			PowerCmd.Remove<HungerPower>(base.Creature);
		}
		if (base.Creature.HasPower<ScrutinyPower>())
		{
			PowerCmd.Remove<ScrutinyPower>(base.Creature);
		}
		if (base.Creature.HasPower<GraspPower>())
		{
			PowerCmd.Remove<GraspPower>(base.Creature);
		}
		PowerCmd.Apply<T>(base.Creature, 1m, base.Creature, null);
	}
}

