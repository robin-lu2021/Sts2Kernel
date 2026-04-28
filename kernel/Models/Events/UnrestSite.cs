using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Runs;

namespace MegaCrit.Sts2.Core.Models.Events;

public sealed class UnrestSite : EventModel
{
	private const string _maxHpLossKey = "MaxHpLoss";

	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlyArray<DynamicVar>(new DynamicVar[2]
	{
		new HealVar(0m),
		new DynamicVar("MaxHpLoss", 8m)
	});

	public override bool IsAllowed(IRunState runState)
	{
		return runState.Players.All((Player p) => (decimal)p.Creature.CurrentHp <= (decimal)p.Creature.MaxHp * 0.70m);
	}

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		return new global::_003C_003Ez__ReadOnlyArray<EventOption>(new EventOption[2]
		{
			new EventOption(this, Rest, "UNREST_SITE.pages.INITIAL.options.REST", KernelHoverTipFactory.FromCardWithCardHoverTips<PoorSleep>()),
			new EventOption(this, Kill, "UNREST_SITE.pages.INITIAL.options.KILL").ThatDecreasesMaxHp(base.DynamicVars["MaxHpLoss"].BaseValue)
		});
	}

	public override void CalculateVars()
	{
		base.DynamicVars.Heal.BaseValue = base.Owner.Creature.MaxHp - base.Owner.Creature.CurrentHp;
	}

	private void Rest()
	{
		CreatureCmd.Heal(base.Owner.Creature, base.DynamicVars.Heal.BaseValue);
		CardPileCmd.AddCursesToDeck(new global::_003C_003Ez__ReadOnlySingleElementList<CardModel>(KernelModelDb.Card<PoorSleep>()), base.Owner);
		SetEventFinished(L10NLookup("UNREST_SITE.pages.REST.description"));
	}

	private void Kill()
	{
		CreatureCmd.LoseMaxHp(new ThrowingPlayerChoiceContext(), base.Owner.Creature, base.DynamicVars["MaxHpLoss"].BaseValue, isFromCard: false);
		RelicModel relic = RelicFactory.PullNextRelicFromFront(base.Owner).ToMutable();
		RelicCmd.Obtain(relic, base.Owner);
		SetEventFinished(L10NLookup("UNREST_SITE.pages.KILL.description"));
	}
}

