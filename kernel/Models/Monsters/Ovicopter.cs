using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace MegaCrit.Sts2.Core.Models.Monsters;

public sealed class Ovicopter : MonsterModel
{
	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 126, 124);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 132, 130);

	private int SmashDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 17, 16);

	private int TenderizerDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 8, 7);

	private int NutritionalPasteStrengthAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 4, 3);

	private bool CanLay => base.Creature.CombatState.GetTeammatesOf(base.Creature).Count((Creature c) => c.IsAlive) <= 3;

	public override void AfterAddedToRoom()
	{
		base.AfterAddedToRoom();
	}

	public override void BeforeRemovedFromRoom()
	{
		;
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("LAY_EGGS_MOVE", SyncMove(LayEggsMove), new SummonIntent());
		MoveState moveState2 = new MoveState("SMASH_MOVE", SyncMove(SmashMove), new SingleAttackIntent(SmashDamage));
		MoveState moveState3 = new MoveState("TENDERIZER_MOVE", SyncMove(TenderizerMove), new SingleAttackIntent(TenderizerDamage), new DebuffIntent());
		MoveState moveState4 = new MoveState("NUTRITIONAL_PASTE_MOVE", SyncMove(NutritionalPasteMove), new BuffIntent());
		ConditionalBranchState conditionalBranchState = new ConditionalBranchState("SUMMON_BRANCH_STATE");
		moveState.FollowUpState = moveState2;
		moveState4.FollowUpState = moveState2;
		moveState2.FollowUpState = moveState3;
		moveState3.FollowUpState = conditionalBranchState;
		conditionalBranchState.AddState(moveState, () => CanLay);
		conditionalBranchState.AddState(moveState4, () => !CanLay);
		list.Add(moveState4);
		list.Add(moveState);
		list.Add(moveState2);
		list.Add(moveState3);
		list.Add(conditionalBranchState);
		return new MonsterMoveStateMachine(list, moveState);
	}

	private void LayEggsMove(IReadOnlyList<Creature> targets)
	{
		for (int i = 0; i < 3; i++)
		{
			string slotName = base.CombatState.Encounter.Slots.LastOrDefault((string s) => base.CombatState.Enemies.All((Creature c) => c.SlotName != s), string.Empty);
			PowerCmd.Apply<MinionPower>(CreatureCmd.Add<ToughEgg>(base.CombatState, slotName), 1m, base.Creature, null);
		}
	}

	private void NutritionalPasteMove(IReadOnlyList<Creature> targets)
	{
		PowerCmd.Apply<StrengthPower>(base.Creature, NutritionalPasteStrengthAmount, base.Creature, null);
	}

	private void SmashMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(SmashDamage).FromMonster(this)
			.Execute(null);
	}

	private void TenderizerMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(TenderizerDamage).FromMonster(this)
			.Execute(null);
		PowerCmd.Apply<VulnerablePower>(targets, 2m, base.Creature, null);
	}

	
}

