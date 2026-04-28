using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Runs;

namespace MegaCrit.Sts2.Core.Models.Events;

public sealed class GraveOfTheForgotten : EventModel
{
	private const string _enchantmentKey = "Enchantment";

	private const string _relicKey = "Relic";

	private const string _curseKey = "Curse";

	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlyArray<DynamicVar>(new DynamicVar[3]
	{
		new StringVar("Relic", KernelModelDb.Relic<ForgottenSoul>().Title.GetFormattedText()),
		new StringVar("Enchantment", ModelDb.Enchantment<SoulsPower>().Title.GetFormattedText()),
		new StringVar("Curse", KernelModelDb.Card<Decay>().Title)
	});

	public override bool IsAllowed(IRunState runState)
	{
		return runState.Players.All(HasEnchantableCards);
	}

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		EventOption eventOption = ((!HasEnchantableCards(base.Owner)) ? new EventOption(this, null, "GRAVE_OF_THE_FORGOTTEN.pages.INITIAL.options.CONFRONT_LOCKED") : new EventOption(this, Confront, "GRAVE_OF_THE_FORGOTTEN.pages.INITIAL.options.CONFRONT", HoverTipFactory.FromEnchantment<SoulsPower>().Concat(HoverTipFactory.FromCardWithCardHoverTips<Decay>())));
		return new global::_003C_003Ez__ReadOnlyArray<EventOption>(new EventOption[2]
		{
			eventOption,
			new EventOption(this, Accept, "GRAVE_OF_THE_FORGOTTEN.pages.INITIAL.options.ACCEPT", KernelHoverTipFactory.FromRelic<ForgottenSoul>())
		});
	}

	private void Confront()
	{
		CardPileCmd.AddCurseToDeck<Decay>(base.Owner);
		CardModel? cardModel = CardSelectCmd.FromDeckForEnchantment(base.Owner, ModelDb.Enchantment<SoulsPower>(), 1, new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 1)).FirstOrDefault();
		if (cardModel != null)
		{
			CardCmd.Enchant<SoulsPower>(cardModel, 1m);
		}
		SetEventFinished(L10NLookup("GRAVE_OF_THE_FORGOTTEN.pages.CONFRONT.description"));
	}

	private void Accept()
	{
		RelicCmd.Obtain<ForgottenSoul>(base.Owner);
		SetEventFinished(L10NLookup("GRAVE_OF_THE_FORGOTTEN.pages.ACCEPT.description"));
	}

	private bool HasEnchantableCards(Player player)
	{
		IReadOnlyList<CardModel> cards = PileType.Deck.GetPile(player).Cards;
		return cards.Any((CardModel c) => ModelDb.Enchantment<SoulsPower>().CanEnchant(c));
	}
}

