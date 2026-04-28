using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Runs;

namespace MegaCrit.Sts2.Core.Models.Events;

public sealed class Amalgamator : EventModel
{
	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlyArray<DynamicVar>(new DynamicVar[2]
	{
		new StringVar("Card1", new UltimateStrike().Title),
		new StringVar("Card2", new UltimateDefend().Title)
	});

	public override bool IsAllowed(IRunState runState)
	{
		return runState.Players.All((Player p) => p.Deck.Cards.OfType<CardModel>().Count((CardModel c) => IsValid(CardTag.Strike, c)) >= 2 && p.Deck.Cards.OfType<CardModel>().Count((CardModel c) => IsValid(CardTag.Defend, c)) >= 2);
	}

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		return new global::_003C_003Ez__ReadOnlyArray<EventOption>(new EventOption[2]
		{
			new EventOption(this, CombineStrikes, InitialOptionKey("COMBINE_STRIKES")),
			new EventOption(this, CombineDefends, InitialOptionKey("COMBINE_DEFENDS"))
		});
	}

	private void CombineStrikes()
	{
		List<CardModel> cards = CardSelectCmd.FromDeckForRemoval(prefs: new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 2), player: base.Owner, filter: (CardModel c) => IsValid(CardTag.Strike, c)).ToList();
		CardPileCmd.RemoveFromDeck(cards, showPreview: false);
		CardModel card = base.Owner.RunState.CreateCard<UltimateStrike>(base.Owner);
		CardPileCmd.Add(card, PileType.Deck);
		SetEventFinished(L10NLookup("AMALGAMATOR.pages.COMBINE_STRIKES.description"));
	}

	private void CombineDefends()
	{
		List<CardModel> cards = CardSelectCmd.FromDeckForRemoval(prefs: new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 2), player: base.Owner, filter: (CardModel c) => IsValid(CardTag.Defend, c)).ToList();
		CardPileCmd.RemoveFromDeck(cards, showPreview: false);
		CardModel card = base.Owner.RunState.CreateCard<UltimateDefend>(base.Owner);
		CardPileCmd.Add(card, PileType.Deck);
		SetEventFinished(L10NLookup("AMALGAMATOR.pages.COMBINE_DEFENDS.description"));
	}

	private static bool IsValid(CardTag tag, CardModel card)
	{
		if (card.Tags.Contains(tag))
		{
			if (card != null && card.Rarity == CardRarity.Basic)
			{
				return card.IsRemovable;
			}
			return false;
		}
		return false;
	}
}
