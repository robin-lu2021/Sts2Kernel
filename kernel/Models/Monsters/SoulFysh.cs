using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace MegaCrit.Sts2.Core.Models.Monsters;

public sealed class SoulFysh : MonsterModel
{
	private const string _attackBeckonTrigger = "AttackBeckon";

	private const string _intangibleStartTrigger = "IntangibleStart";

	private const string _attackDebuffTrigger = "AttackDebuffTrigger";

	private const string _beckonTrigger = "Beckon";

	private bool _isInvisible;

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 221, 211);

	public override int MaxInitialHp => MinInitialHp;

	private int DeGasDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 17, 16);

	private int ScreamDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 12, 11);

	private int GazeDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 8, 7);

	private int BeckonMoveAmount => 2;

	private int GazeMoveAmount => 1;

	private int ScreamMoveAmount => 3;

	public bool IsInvisible
	{
		get
		{
			return _isInvisible;
		}
		set
		{
			AssertMutable();
			_isInvisible = value;
		}
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("BECKON_MOVE", SyncMove(BeckonMove), new StatusIntent(BeckonMoveAmount));
		MoveState moveState2 = new MoveState("DE_GAS_MOVE", SyncMove(DeGasMove), new SingleAttackIntent(DeGasDamage));
		MoveState moveState3 = new MoveState("GAZE_MOVE", SyncMove(GazeMove), new SingleAttackIntent(GazeDamage), new StatusIntent(GazeMoveAmount));
		MoveState moveState4 = new MoveState("FADE_MOVE", SyncMove(FadeMove), new BuffIntent());
		MoveState moveState5 = new MoveState("SCREAM_MOVE", SyncMove(ScreamMove), new SingleAttackIntent(ScreamDamage), new DebuffIntent());
		moveState.FollowUpState = moveState2;
		moveState2.FollowUpState = moveState3;
		moveState3.FollowUpState = moveState4;
		moveState4.FollowUpState = moveState5;
		moveState5.FollowUpState = moveState;
		list.Add(moveState);
		list.Add(moveState2);
		list.Add(moveState3);
		list.Add(moveState5);
		list.Add(moveState4);
		return new MonsterMoveStateMachine(list, moveState);
	}

	private void BeckonMove(IReadOnlyList<Creature> targets)
	{
		foreach (Creature target in targets)
		{
			Player player = target.Player ?? target.PetOwner;
			CardPileAddResult[] statusCards = new CardPileAddResult[BeckonMoveAmount];
			CardModel card = base.CombatState.CreateCard<Beckon>(player);
			CardPileAddResult[] array = statusCards;
			array[0] = CardPileCmd.AddGeneratedCardToCombat(card, PileType.Draw, addedByPlayer: false, CardPilePosition.Random);
			CardModel card2 = base.CombatState.CreateCard<Beckon>(player);
			array = statusCards;
			array[1] = CardPileCmd.AddGeneratedCardToCombat(card2, PileType.Discard, addedByPlayer: false);
			if (LocalContext.IsMe(player))
			{
				CardCmd.PreviewCardPileAdd(statusCards);
			}
		}
	}

	private void GazeMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(GazeDamage).FromMonster(this)
			.Execute(null);
		foreach (Creature target in targets)
		{
			Player player = target.Player ?? target.PetOwner;
			CardPileAddResult[] statusCards = new CardPileAddResult[1];
			CardModel card = base.CombatState.CreateCard<Beckon>(player);
			CardPileAddResult[] array = statusCards;
			array[0] = CardPileCmd.AddGeneratedCardToCombat(card, PileType.Discard, addedByPlayer: false);
			if (LocalContext.IsMe(player))
			{
				CardCmd.PreviewCardPileAdd(statusCards);
			}
		}
	}

	private void DeGasMove(IReadOnlyList<Creature> targets)
	{
		DamageCmd.Attack(DeGasDamage).FromMonster(this)
			.Execute(null);
	}

	private void ScreamMove(IReadOnlyList<Creature> targets)
	{
		IsInvisible = false;
		DamageCmd.Attack(ScreamDamage).FromMonster(this)
			.Execute(null);
		PowerCmd.Apply<VulnerablePower>(targets, ScreamMoveAmount, base.Creature, null);
	}

	private void FadeMove(IReadOnlyList<Creature> targets)
	{
		IsInvisible = true;
		PowerCmd.Apply<IntangiblePower>(base.Creature, 2m, base.Creature, null);
	}

	
}

