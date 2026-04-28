using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Cards;

namespace MegaCrit.Sts2.Core.Models.Events;

public sealed class Reflections : EventModel
{
	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		return new global::_003C_003Ez__ReadOnlyArray<EventOption>(new EventOption[2]
		{
			new EventOption(this, TouchAMirror, "REFLECTIONS.pages.INITIAL.options.TOUCH_A_MIRROR"),
			new EventOption(this, Shatter, "REFLECTIONS.pages.INITIAL.options.SHATTER", KernelHoverTipFactory.FromCardWithCardHoverTips<BadLuck>())
		});
	}

	private void TouchAMirror()
	{
		List<CardModel> upgradedCards = base.Owner.Deck.Cards.Where((CardModel c) => c.IsUpgraded).ToList();
		for (int i = 0; i < 2; i++)
		{
			if (upgradedCards.Count <= 0)
			{
				break;
			}
			CardModel cardModel = base.Rng.NextItem(upgradedCards);
			upgradedCards.Remove(cardModel);
			CardCmd.Downgrade(cardModel);
		}
		List<CardModel> upgradableCards = base.Owner.Deck.Cards.Where((CardModel c) => c.IsUpgradable).ToList();
		for (int i = 0; i < 4; i++)
		{
			if (upgradableCards.Count <= 0)
			{
				break;
			}
			CardModel cardModel2 = base.Rng.NextItem(upgradableCards);
			upgradableCards.Remove(cardModel2);
			CardCmd.Upgrade(cardModel2, CardPreviewStyle.None);
		}
		SetEventFinished(L10NLookup("REFLECTIONS.pages.TOUCH_A_MIRROR.description"));
	}

	private void Shatter()
	{
		int originalDeckSize = base.Owner.Deck.Cards.Count;
		for (int i = 0; i < originalDeckSize; i++)
		{
			CardModel card = base.Owner.RunState.CloneCard(base.Owner.Deck.Cards[i]);
			CardPileCmd.Add(card, PileType.Deck);
		}
		CardPileCmd.AddCurseToDeck<BadLuck>(base.Owner);
		SetEventFinished(L10NLookup("REFLECTIONS.pages.SHATTER.description"));
	}
}
