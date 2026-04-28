using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Random;

namespace MegaCrit.Sts2.Core.Models.Monsters;

public sealed class FakeMerchantMonster : MonsterModel
{
	private const string _spewCoinsTrigger = "spew";

	private const string _throwRelicTrigger = "throw";

	private const int _spewCoinsDamage = 2;

	private const int _spewCoinsRepeat = 8;

	private const string _attackMultiTrigger = "attack_multi";

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 175, 165);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 175, 165);

	private int SwipeDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 15, 13);

	private int ThrowRelicDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 10, 9);

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("SWIPE_MOVE", SwipeMove, new SingleAttackIntent(SwipeDamage));
		MoveState moveState2 = new MoveState("SPEW_COINS_MOVE", SpewCoinsMove, new MultiAttackIntent(2, 8));
		MoveState moveState3 = new MoveState("THROW_RELIC_MOVE", ThrowRelicMove, new SingleAttackIntent(ThrowRelicDamage), new DebuffIntent());
		MoveState moveState4 = new MoveState("ENRAGE_MOVE", EnrageMove, new BuffIntent());
		RandomBranchState randomBranchState = new RandomBranchState("RAND_MOVE");
		randomBranchState.AddBranch(moveState, MoveRepeatType.CannotRepeat);
		randomBranchState.AddBranch(moveState2, MoveRepeatType.CannotRepeat);
		randomBranchState.AddBranch(moveState3, MoveRepeatType.CannotRepeat);
		randomBranchState.AddBranch(moveState4, 3, MoveRepeatType.CannotRepeat);
		moveState.FollowUpState = randomBranchState;
		moveState2.FollowUpState = randomBranchState;
		moveState4.FollowUpState = randomBranchState;
		RandomBranchState randomBranchState2 = new RandomBranchState("RAND_ATTACK_MOVE");
		randomBranchState2.AddBranch(moveState, MoveRepeatType.CannotRepeat);
		randomBranchState2.AddBranch(moveState2, MoveRepeatType.CannotRepeat);
		randomBranchState2.AddBranch(moveState3, MoveRepeatType.CannotRepeat);
		moveState3.FollowUpState = randomBranchState2;
		list.Add(moveState);
		list.Add(moveState2);
		list.Add(moveState3);
		list.Add(moveState4);
		list.Add(randomBranchState);
		list.Add(randomBranchState2);
		return new MonsterMoveStateMachine(list, moveState);
	}

	private void SwipeMove(IReadOnlyList<Creature> targets)
	{
		ShowDialogueForMove("SWIPE");
		DamageCmd.Attack(SwipeDamage).FromMonster(this)
			.Execute(null);
	}

	private void SpewCoinsMove(IReadOnlyList<Creature> targets)
	{
		ShowDialogueForMove("SPEW_COINS");
		DamageCmd.Attack(2m).FromMonster(this).WithHitCount(8)
			.Execute(null);
	}

	private void ThrowRelicMove(IReadOnlyList<Creature> targets)
	{
		ShowDialogueForMove("THROW_RELIC");
		DamageCmd.Attack(ThrowRelicDamage).FromMonster(this)
			.Execute(null);
		PowerCmd.Apply<FrailPower>(targets, 1m, base.Creature, null);
	}

	private void EnrageMove(IReadOnlyList<Creature> targets)
	{
		ShowDialogueForMove("ENRAGE");
		PowerCmd.Apply<StrengthPower>(base.Creature, 2m, base.Creature, null);
	}
	
	private void ShowDialogueForMove(string moveId)
	{
		LocString locString = MegaCrit.Sts2.Core.Random.Rng.Chaotic.NextItem(GetLinesForMove(moveId));
	}

	private IEnumerable<LocString> GetLinesForMove(string moveId)
	{
		LocTable table = LocManager.Instance.GetTable("monsters");
		return table.GetLocStringsWithPrefix(base.Id.Entry + ".moves." + moveId + ".speakLine");
	}
}

