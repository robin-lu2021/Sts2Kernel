using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Saves;

namespace MegaCrit.Sts2.Core.Models.Monsters;

public sealed class TestSubject : MonsterModel
{
	private const string _testSubjectCustomTrackName = "test_subject_progress";

	private const int _baseTestSubjectNum = 8;

	private const int _phase3LacerateRepeat = 3;

	private const string _growthSpurtTrigger = "GrowthSpurtTrigger";

	private const string _bigAttackTrigger = "BiteTrigger";

	private const string _multiAttackTrigger = "MultiAttackTrigger";

	private const string _deadTrigger = "DeadTrigger";

	private const string _respawnTrigger = "RespawnTrigger";

	private const string _burnTrigger = "BurnTrigger";

	private MoveState _deadState;

	private int _respawns;

	private int _extraMultiClawCount;

	public override LocString Title
	{
		get
		{
			LocString title = base.Title;
			title.Add("Count", SaveManager.Instance.Progress.TestSubjectKills + 8);
			return title;
		}
	}

	public override int MinInitialHp => FirstFormHp;

	public override int MaxInitialHp => MinInitialHp;

	public int FirstFormHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 111, 100);

	public int SecondFormHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 212, 200);

	public int ThirdFormHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 313, 300);

	private int EnrageAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 3, 2);

	private int BiteDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 22, 20);

	private int SkullBashDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 16, 14);

	private int MultiClawDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 11, 10);

	private int BaseMultiClawCount => 3;

	private int Phase3LacerateDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 11, 10);

	private int BigPounceDamage => 45;

	private int BurningGrowlBurnCount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 5, 3);

	private int BurningGrowlStrengthGain => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 3, 2);

	private MoveState DeadState
	{
		get
		{
			return _deadState;
		}
		set
		{
			AssertMutable();
			_deadState = value;
		}
	}

	private int Respawns
	{
		get
		{
			return _respawns;
		}
		set
		{
			AssertMutable();
			_respawns = value;
		}
	}

	private int ExtraMultiClawCount
	{
		get
		{
			return _extraMultiClawCount;
		}
		set
		{
			AssertMutable();
			_extraMultiClawCount = value;
		}
	}

	private int MultiClawTotalCount => BaseMultiClawCount + ExtraMultiClawCount;

	public override bool ShouldDisappearFromDoom => Respawns >= 2;

	public void TriggerDeadState()
	{
		base.CombatState.RunState.ExtraFields.TestSubjectKills++;
		SetMoveImmediate(DeadState, forceTransition: true);
	}

	public override void AfterAddedToRoom()
	{
		base.AfterAddedToRoom();
		PowerCmd.Apply<AdaptablePower>(base.Creature, 1m, base.Creature, null);
		PowerCmd.Apply<EnragePower>(base.Creature, EnrageAmount, base.Creature, null);
	}

	public override void BeforeRemovedFromRoom()
	{
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		DeadState = new MoveState("RESPAWN_MOVE", RespawnMove, new HealIntent(), new BuffIntent())
		{
			MustPerformOnceBeforeTransitioning = true
		};
		MoveState moveState = new MoveState("BITE_MOVE", BiteMove, new SingleAttackIntent(BiteDamage));
		MoveState moveState2 = new MoveState("SKULL_BASH_MOVE", SkullBashMove, new SingleAttackIntent(SkullBashDamage), new DebuffIntent());
		MoveState moveState3 = new MoveState("MULTI_CLAW_MOVE", MultiClawMove, new MultiAttackIntent(MultiClawDamage, () => MultiClawTotalCount));
		MoveState moveState4 = new MoveState("PHASE3_LACERATE_MOVE", Phase3LacerateMove, new MultiAttackIntent(Phase3LacerateDamage, 3));
		MoveState moveState5 = new MoveState("BIG_POUNCE_MOVE", BigPounceMove, new SingleAttackIntent(BigPounceDamage));
		MoveState moveState6 = new MoveState("BURNING_GROWL_MOVE", BurningGrowlMove, new StatusIntent(BurningGrowlBurnCount), new BuffIntent());
		ConditionalBranchState conditionalBranchState = new ConditionalBranchState("REVIVE_BRANCH");
		moveState.FollowUpState = moveState2;
		moveState2.FollowUpState = moveState;
		moveState3.FollowUpState = moveState3;
		moveState4.FollowUpState = moveState5;
		moveState5.FollowUpState = moveState6;
		moveState6.FollowUpState = moveState4;
		DeadState.FollowUpState = conditionalBranchState;
		conditionalBranchState.AddState(moveState3, () => Respawns < 2);
		conditionalBranchState.AddState(moveState4, () => Respawns >= 2);
		list.Add(DeadState);
		list.Add(moveState);
		list.Add(moveState2);
		list.Add(moveState3);
		list.Add(moveState4);
		list.Add(moveState5);
		list.Add(moveState6);
		list.Add(conditionalBranchState);
		return new MonsterMoveStateMachine(list, moveState);
	}

	private void RespawnMove(IReadOnlyList<Creature> targets)
	{
		Respawns++;
		if (base.Creature.CombatState != null)
		{
			base.Creature.GetPower<AdaptablePower>()?.DoRevive();
			switch (Respawns)
			{
			case 1:
				Revive(SecondFormHp);
				PowerCmd.Apply<PainfulStabsPower>(base.Creature, 1m, base.Creature, null);
				break;
			case 2:
				Revive(ThirdFormHp);
				PowerCmd.Apply<NemesisPower>(base.Creature, 1m, base.Creature, null);
				PowerCmd.Remove<AdaptablePower>(base.Creature);
				PowerCmd.Remove<PainfulStabsPower>(base.Creature);
				break;
			}
		}
	}

	private void BiteMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(BiteDamage).FromMonster(this)
			.Execute(null);
	}

	private void SkullBashMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(SkullBashDamage).FromMonster(this)
			.Execute(null);
		PowerCmd.Apply<VulnerablePower>(targets, 1m, base.Creature, null);
	}

	private void MultiClawMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(MultiClawDamage).WithHitCount(MultiClawTotalCount).FromMonster(this)
			.Execute(null);
		ExtraMultiClawCount++;
	}

	private void Phase3LacerateMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(Phase3LacerateDamage).WithHitCount(3).FromMonster(this)
			.Execute(null);
	}

	private void BigPounceMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(BigPounceDamage).FromMonster(this)
			.Execute(null);
	}

	private void BurningGrowlMove(IReadOnlyList<Creature> targets)
	{
		CardPileCmd.AddToCombatAndPreview<Burn>(targets, PileType.Discard, BurningGrowlBurnCount, addedByPlayer: false);
		PowerCmd.Apply<StrengthPower>(base.Creature, BurningGrowlStrengthGain, base.Creature, null);
	}

	private void Revive(int baseRespawnHp)
	{
		AssertMutable();
		int scaledHp = baseRespawnHp * base.Creature.CombatState.Players.Count;
		CreatureCmd.SetMaxHp(base.Creature, scaledHp);
		CreatureCmd.Heal(base.Creature, scaledHp);
	}
}

