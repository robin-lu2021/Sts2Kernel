using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.PotionPools;
using MegaCrit.Sts2.Core.Rewards;

namespace MegaCrit.Sts2.Core.Models.Events;

public sealed class Wellspring : EventModel
{
	private const string _batheKey = "BatheCurses";

	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlySingleElementList<DynamicVar>(new DynamicVar("BatheCurses", 1m));

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		return new global::_003C_003Ez__ReadOnlyArray<EventOption>(new EventOption[2]
		{
			new EventOption(this, Bottle, "WELLSPRING.pages.INITIAL.options.BOTTLE"),
			new EventOption(this, Bathe, "WELLSPRING.pages.INITIAL.options.BATHE")
		});
	}

	private void Bottle()
	{
		IEnumerable<MegaCrit.Sts2.Core.PotionModel> items = base.Owner.Character.PotionPool.GetUnlockedPotions(base.Owner.UnlockState).Concat(ModelDb.PotionPool<SharedPotionPool>().GetUnlockedPotions(base.Owner.UnlockState));
		MegaCrit.Sts2.Core.PotionModel potionModel = base.Owner.PlayerRng.Rewards.NextItem(items);
		if (potionModel != null)
		{
			RewardsCmd.OfferCustom(base.Owner, new List<Reward>(1)
			{
				new PotionReward(potionModel.ToMutable(), base.Owner)
			});
		}
		SetEventFinished(L10NLookup("WELLSPRING.pages.BOTTLE.description"));
	}

	private void Bathe()
	{
		List<CardModel> cards = (CardSelectCmd.FromDeckForRemoval(prefs: new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 1), player: base.Owner).ToList());
		CardPileCmd.RemoveFromDeck(cards);
		AddGuilty(base.DynamicVars["BatheCurses"].IntValue);
		SetEventFinished(L10NLookup("WELLSPRING.pages.BATHE.description"));
	}

	private void AddGuilty(int amount)
	{
		CardPileCmd.AddCursesToDeck(Enumerable.Repeat(KernelModelDb.Card<Guilty>(), amount), base.Owner);
	}
}
