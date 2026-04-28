using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;

namespace MegaCrit.Sts2.Core.Models.Events;

public sealed class Bugslayer : EventModel
{
	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlyArray<DynamicVar>(new DynamicVar[2]
	{
		new StringVar("Card1", KernelModelDb.Card<Exterminate>().Title),
		new StringVar("Card2", KernelModelDb.Card<Squash>().Title)
	});

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		return new global::_003C_003Ez__ReadOnlyArray<EventOption>(new EventOption[2]
		{
			new EventOption(this, Extermination, "BUGSLAYER.pages.INITIAL.options.EXTERMINATION", KernelHoverTipFactory.FromCardWithCardHoverTips<Exterminate>()),
			new EventOption(this, Squash, "BUGSLAYER.pages.INITIAL.options.SQUASH", KernelHoverTipFactory.FromCardWithCardHoverTips<Squash>())
		});
	}

	private void Extermination()
	{
		AddAndPreview<Exterminate>(L10NLookup("BUGSLAYER.pages.EXTERMINATION.description"));
	}

	private void Squash()
	{
		AddAndPreview<Squash>(L10NLookup("BUGSLAYER.pages.SQUASH.description"));
	}

	private void AddAndPreview<T>(LocString loc) where T : CardModel, new()
	{
		CardModel card = KernelCardFactoryExtensions.CreateCard<T>(base.Owner.RunState, base.Owner);
		CardPileCmd.Add(card, PileType.Deck);
		SetEventFinished(loc);
	}
}

