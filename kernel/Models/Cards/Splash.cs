using MegaCrit.Sts2.Core;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace MegaCrit.Sts2.Core.Models.Cards;

public sealed class Splash : CardModel
{
	private CardModel? _mockGeneratedCard;

	public Splash()
		: base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
	{
	}

	protected override void OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		CardModel cardModel;
		if (_mockGeneratedCard == null)
		{
			List<CardPoolModel> list = base.Owner.UnlockState.CharacterCardPools.ToList();
			if (list.Count > 1)
			{
				list.Remove(base.Owner.Character.CardPool);
			}
			IEnumerable<CardModel> cards = from c in list.SelectMany((CardPoolModel c) => c.GetUnlockedCards(base.Owner.UnlockState, base.Owner.RunState.CardMultiplayerConstraint))
				where c.Type == CardType.Attack
				select c;
			List<CardModel> list2 = KernelCardFactoryExtensions.GetDistinctForCombat(base.Owner, cards, 3, base.Owner.RunState.Rng.CombatCardGeneration).ToList();
			if (base.IsUpgraded)
			{
				foreach (CardModel item in list2)
				{
					CardCmd.Upgrade(item);
				}
			}
			cardModel = CardSelectCmd.FromChooseACardScreen(choiceContext, list2, base.Owner, canSkip: true);
		}
		else
		{
			cardModel = _mockGeneratedCard;
			if (base.IsUpgraded)
			{
				CardCmd.Upgrade(cardModel);
			}
		}
		if (cardModel != null)
		{
			cardModel.SetToFreeThisTurn();
			CardPileCmd.AddGeneratedCardToCombat(cardModel, PileType.Hand, addedByPlayer: true);
		}
	}

	public void MockGeneratedCard(CardModel card)
	{
		AssertMutable();
		_mockGeneratedCard = card;
	}
}

