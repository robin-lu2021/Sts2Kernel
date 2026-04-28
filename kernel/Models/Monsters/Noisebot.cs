using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.TestSupport;

namespace MegaCrit.Sts2.Core.Models.Monsters;

public sealed class Noisebot : MonsterModel
{
	private const int _noiseStatusCount = 2;

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 19, 18);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 24, 23);

	public override void AfterAddedToRoom()
	{
		base.AfterAddedToRoom();
		return;
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		List<MonsterState> list = new List<MonsterState>();
		MoveState moveState = new MoveState("NOISE_MOVE", SyncMove(NoiseMove), new StatusIntent(2));
		moveState.FollowUpState = moveState;
		list.Add(moveState);
		return new MonsterMoveStateMachine(list, moveState);
	}

	private void NoiseMove(IReadOnlyList<Creature> targets)
	{
		foreach (Creature target in targets)
		{
			Player player = target.Player ?? target.PetOwner;
			CardPileAddResult[] statusCards = new CardPileAddResult[2];
			CardModel card = base.CombatState.CreateCard<Dazed>(player);
			CardPileAddResult[] array = statusCards;
			array[0] = CardPileCmd.AddGeneratedCardToCombat(card, PileType.Discard, addedByPlayer: false);
			CardModel card2 = base.CombatState.CreateCard<Dazed>(player);
			array = statusCards;
			array[1] = CardPileCmd.AddGeneratedCardToCombat(card2, PileType.Draw, addedByPlayer: false, CardPilePosition.Random);
			if (LocalContext.IsMe(player))
			{
				CardCmd.PreviewCardPileAdd(statusCards);
			}
		}
	}
}

