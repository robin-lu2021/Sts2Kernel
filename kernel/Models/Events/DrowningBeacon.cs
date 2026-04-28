using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Rewards;

namespace MegaCrit.Sts2.Core.Models.Events;

public sealed class DrowningBeacon : EventModel
{
	private const string _potionKey = "Potion";

	private const string _relicKey = "Relic";

	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlyArray<DynamicVar>(new DynamicVar[3]
	{
		new HpLossVar(13m),
		new StringVar("Potion", KernelModelDb.Potion<GlowwaterPotion>().Title.GetFormattedText()),
		new StringVar("Relic", KernelModelDb.Relic<FresnelLens>().Title.GetFormattedText())
	});

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		return new global::_003C_003Ez__ReadOnlyArray<EventOption>(new EventOption[2]
		{
			new EventOption(this, BottleOption, "DROWNING_BEACON.pages.INITIAL.options.BOTTLE", KernelHoverTipFactory.FromPotion(KernelModelDb.Potion<GlowwaterPotion>())),
			new EventOption(this, ClimbOption, "DROWNING_BEACON.pages.INITIAL.options.CLIMB", KernelHoverTipFactory.FromRelic<FresnelLens>().ThatDecreasesMaxHp(base.DynamicVars.HpLoss.BaseValue))
		});
	}

	private void BottleOption()
	{
		PotionCmd.TryToProcure(KernelModelDb.Potion<GlowwaterPotion>().ToMutable(), base.Owner);
		SetEventFinished(L10NLookup("DROWNING_BEACON.pages.BOTTLE.description"));
	}

	private void ClimbOption()
	{
		CreatureCmd.LoseMaxHp(new ThrowingPlayerChoiceContext(), base.Owner.Creature, base.DynamicVars.HpLoss.BaseValue, isFromCard: false);
		RelicCmd.Obtain(KernelModelDb.Relic<FresnelLens>().ToMutable(), base.Owner);
		SetEventFinished(L10NLookup("DROWNING_BEACON.pages.CLIMB.description"));
	}
}

