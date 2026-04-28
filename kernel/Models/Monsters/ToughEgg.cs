using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.TestSupport;

namespace MegaCrit.Sts2.Core.Models.Monsters;

public sealed class ToughEgg : MonsterModel
{
	private bool _hatched;

	private const string _hatchTrigger = "Hatch";

	private static readonly string[] _eggOptions = new string[2] { "egg1", "egg2" };

	private MonsterState? _afterHatchedState;

	private bool _isHatched;

	public override LocString Title
	{
		get
		{
			if (!_hatched)
			{
				return MonsterModel.L10NMonsterLookup(base.Id.Entry + ".name");
			}
			return MonsterModel.L10NMonsterLookup("HATCHLING.name");
		}
	}

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 15, 14);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 19, 18);

	public int HatchlingMinHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 20, 19);

	public int HatchlingMaxHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 23, 22);

	private static int NibbleDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 5, 4);

	public MonsterState? AfterHatchedState
	{
		get
		{
			return _afterHatchedState;
		}
		set
		{
			AssertMutable();
			_afterHatchedState = value;
		}
	}

	public bool IsHatched
	{
		get
		{
			return _isHatched;
		}
		set
		{
			AssertMutable();
			_isHatched = value;
		}
	}

	public override void AfterAddedToRoom()
	{
		base.AfterAddedToRoom();
		if (!IsHatched)
		{
			int num = ((base.CombatState.CurrentSide != CombatSide.Enemy) ? 1 : 2);
			PowerCmd.Apply<HatchPower>(base.Creature, num, base.Creature, null);
		}
		else
		{
			Hatch();
			base.MoveStateMachine?.ForceCurrentState(AfterHatchedState);
		}
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("HATCH_MOVE", SyncMove(HatchMove), new SummonIntent());
		MoveState moveState2 = (MoveState)(moveState.FollowUpState = new MoveState("NIBBLE_MOVE", SyncMove(NibbleMove), new SingleAttackIntent(NibbleDamage)));
		moveState2.FollowUpState = moveState2;
		list.Add(moveState);
		list.Add(moveState2);
		AfterHatchedState = moveState2;
		return new MonsterMoveStateMachine(list, moveState);
	}

	private void HatchMove(IReadOnlyList<Creature> targets)
	{
		IsHatched = true;
		PowerCmd.Remove<HatchPower>(base.Creature);
		_hatched = true;
		List<PowerModel> list = base.Creature.Powers.Where((PowerModel p) => !(p is MinionPower)).ToList();
		foreach (PowerModel item in list)
		{
			PowerCmd.Remove(item);
		}
		Hatch();
	}

	private void Hatch()
	{
		decimal amount = MegaCrit.Sts2.Core.Entities.Creatures.Creature.ScaleHpForMultiplayer(base.RunRng.Niche.NextInt(HatchlingMinHp, HatchlingMaxHp), base.CombatState.Encounter, base.Creature.CombatState.Players.Count, base.Creature.CombatState.Players[0].RunState.CurrentActIndex);
		CreatureCmd.SetMaxAndCurrentHp(base.Creature, amount);
	}

	private void NibbleMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(NibbleDamage).FromMonster(this)
			.Execute(null);
	}
}

