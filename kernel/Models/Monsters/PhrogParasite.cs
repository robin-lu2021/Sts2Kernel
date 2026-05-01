using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Debug;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
namespace MegaCrit.Sts2.Core.Models.Monsters;

public sealed class PhrogParasite : MonsterModel
{
	private const int _lashRepeat = 4;

	private const int _infestAmt = 3;

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 66, 61);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 68, 64);

	private int LashDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 5, 4);

	public override void AfterAddedToRoom()
	{
		base.AfterAddedToRoom();
		PhrogDebug.LogInfo($"AfterAddedToRoom before apply: combatId={base.Creature.CombatId?.ToString() ?? "null"}, slot={base.Creature.SlotName ?? "null"}, canReceive={base.Creature.CanReceivePowers}, combatInProgress={CombatManager.Instance.IsInProgress}, combatEnding={CombatManager.Instance.IsEnding}");
		PowerCmd.Apply<InfestedPower>(base.Creature, 4m, base.Creature, null);
		PhrogDebug.LogInfo($"AfterAddedToRoom after apply: hasInfested={base.Creature.HasPower<InfestedPower>()}, powers={string.Join(",", base.Creature.Powers.Select(p => p.Id.Entry + ":" + p.Amount))}");
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("INFECT_MOVE", SyncMove(InfectMove), new StatusIntent(3));
		MoveState moveState2 = new MoveState("LASH_MOVE", SyncMove(LashMove), new MultiAttackIntent(LashDamage, 4));
		RandomBranchState randomBranchState = new RandomBranchState("RAND");
		moveState.FollowUpState = moveState2;
		moveState2.FollowUpState = moveState;
		randomBranchState.AddBranch(moveState, MoveRepeatType.CannotRepeat);
		randomBranchState.AddBranch(moveState2, MoveRepeatType.CannotRepeat);
		list.Add(moveState);
		list.Add(moveState2);
		list.Add(randomBranchState);
		return new MonsterMoveStateMachine(list, moveState);
	}

	private void LashMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(LashDamage).WithHitCount(4).FromMonster(this)
			.Execute(null);
	}

	private void InfectMove(IReadOnlyList<Creature> targets)
	{
		CardPileCmd.AddToCombatAndPreview<Infection>(targets, PileType.Discard, 3, addedByPlayer: false);
	}
}

