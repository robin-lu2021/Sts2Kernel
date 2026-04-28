using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Runs;

namespace MegaCrit.Sts2.Core.Models.Events;

public sealed class WoodCarvings : EventModel
{
	private const string _birdCardKey = "BirdCard";

	private const string _snakeEnchantmentKey = "SnakeEnchantment";

	private const string _toricCardKey = "ToricCard";

	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlyArray<DynamicVar>(new DynamicVar[3]
	{
		new StringVar("BirdCard", new Peck().Title),
		new StringVar("SnakeEnchantment", ModelDb.Enchantment<Slither>().Title.GetFormattedText()),
		new StringVar("ToricCard", new ToricToughness().Title)
	});

	public override bool IsAllowed(IRunState runState)
	{
		return runState.Players.All((Player p) => CardPile.Get(PileType.Deck, p).Cards.OfType<CardModel>().Any((CardModel c) => c.Rarity == CardRarity.Basic && c.IsRemovable));
	}

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		IReadOnlyList<CardModel> cards = PileType.Deck.GetPile(base.Owner).Cards.OfType<CardModel>().ToList();
		EventOption eventOption = ((!cards.Any((CardModel c) => ModelDb.Enchantment<Slither>().CanEnchant(c))) ? new EventOption(this, null, "WOOD_CARVINGS.pages.INITIAL.options.SNAKE_LOCKED") : new EventOption(this, Snake, "WOOD_CARVINGS.pages.INITIAL.options.SNAKE"));
		return new global::_003C_003Ez__ReadOnlyArray<EventOption>(new EventOption[3]
		{
			new EventOption(this, Bird, "WOOD_CARVINGS.pages.INITIAL.options.BIRD"),
			eventOption,
			new EventOption(this, Torus, "WOOD_CARVINGS.pages.INITIAL.options.TORUS")
		});
	}

	private void Bird()
	{
		CardModel cardModel = RunSynchronously(CardSelectCmd.FromDeckGeneric(base.Owner, new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, 1), (CardModel c) => c.IsTransformable && c.Rarity == CardRarity.Basic).FirstOrDefault());
		if (cardModel != null)
		{
			CardCmd.TransformTo<Peck>(cardModel, CardPreviewStyle.None);
		}
		SetEventFinished(L10NLookup("WOOD_CARVINGS.pages.BIRD.description"));
	}

	private void Snake()
	{
		CardModel? cardModel = CardSelectCmd.FromDeckForEnchantment(base.Owner, ModelDb.Enchantment<Slither>(), 1, new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 1)).FirstOrDefault();
		if (cardModel != null)
		{
			CardCmd.Enchant<Slither>(cardModel, 1m);
		}
		SetEventFinished(L10NLookup("WOOD_CARVINGS.pages.SNAKE.description"));
	}

	private void Torus()
	{
		CardModel cardModel = RunSynchronously(CardSelectCmd.FromDeckGeneric(base.Owner, new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, 1), (CardModel c) => c != null && c.IsTransformable && c.Rarity == CardRarity.Basic).FirstOrDefault());
		if (cardModel != null)
		{
			CardCmd.TransformTo<ToricToughness>(cardModel, CardPreviewStyle.None);
		}
		SetEventFinished(L10NLookup("WOOD_CARVINGS.pages.TORUS.description"));
	}
}
