using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Relics;

namespace MegaCrit.Sts2.Core.Models.Events;

public sealed class LostWisp : EventModel
{
	private const string _relicKey = "Relic";

	private const string _curseKey = "Curse";

	private const int _baseGold = 60;

	private const int _goldVariance = 15;

	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlyArray<DynamicVar>(new DynamicVar[3]
	{
		new GoldVar(60),
		new StringVar("Relic", KernelModelDb.Relic<MegaCrit.Sts2.Core.Models.Relics.LostWisp>().Title.GetFormattedText()),
		new StringVar("Curse", KernelModelDb.Card<Decay>().Title)
	});

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		EventOption[] array = new EventOption[2];
		Action onChosen = Claim;
		List<IHoverTip> list = new List<IHoverTip>();
		list.AddRange(KernelHoverTipFactory.FromRelic<MegaCrit.Sts2.Core.Models.Relics.LostWisp>());
		list.AddRange(KernelHoverTipFactory.FromCardWithCardHoverTips<Decay>());
		array[0] = new EventOption(this, onChosen, "LOST_WISP.pages.INITIAL.options.CLAIM", list.ToArray());
		array[1] = new EventOption(this, Search, "LOST_WISP.pages.INITIAL.options.SEARCH");
		return new global::_003C_003Ez__ReadOnlyArray<EventOption>(array);
	}

	public override void CalculateVars()
	{
		base.DynamicVars.Gold.BaseValue += (decimal)base.Rng.NextInt(-15, 16);
	}

	private void Claim()
	{
		CardPileCmd.AddCursesToDeck(new global::_003C_003Ez__ReadOnlySingleElementList<CardModel>(KernelModelDb.Card<Decay>()), base.Owner);
		RelicCmd.Obtain<MegaCrit.Sts2.Core.Models.Relics.LostWisp>(base.Owner);
		SetEventFinished(L10NLookup("LOST_WISP.pages.CLAIM.description"));
	}

	private void Search()
	{
		PlayerCmd.GainGold(base.DynamicVars.Gold.IntValue, base.Owner);
		SetEventFinished(L10NLookup("LOST_WISP.pages.SEARCH.description"));
	}
}

