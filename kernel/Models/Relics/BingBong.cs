using MegaCrit.Sts2.Core;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;

namespace MegaCrit.Sts2.Core.Models.Relics;

public sealed class BingBong : RelicModel
{
	private HashSet<CardModel>? _cardsToSkip;

	public override RelicRarity Rarity => RelicRarity.Event;

	private HashSet<CardModel> CardsToSkip
	{
		get
		{
			AssertMutable();
			if (_cardsToSkip == null)
			{
				_cardsToSkip = new HashSet<CardModel>();
			}
			return _cardsToSkip;
		}
	}

	public override void AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? source)
	{
		CardPile? pile = card.Pile;
		if (pile != null && pile.Type == PileType.Deck && card.Owner == base.Owner && source == null && !CardsToSkip.Remove(card))
		{
			 
			CardModel cardModel = base.Owner.RunState.CloneCard(card);
			CardsToSkip.Add(cardModel);
			CardCmd.PreviewCardPileAdd(CardPileCmd.Add(cardModel, PileType.Deck, CardPilePosition.Bottom, this));
		}
	}
}