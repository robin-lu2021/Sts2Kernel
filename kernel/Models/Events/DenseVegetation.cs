using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core.Models.Events;

public sealed class DenseVegetation : EventModel
{
	public override bool IsShared => true;

	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlyArray<DynamicVar>(new DynamicVar[3]
	{
		new GoldVar(0),
		new HealVar(0m),
		new HpLossVar(8m)
	});

	public override void CalculateVars()
	{
		base.DynamicVars.Gold.BaseValue = base.Rng.NextInt(61, 100);
		base.DynamicVars.Heal.BaseValue = ((base.Owner != null) ? HealRestSiteOption.GetHealAmount(base.Owner) : 0m);
	}

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		return new global::_003C_003Ez__ReadOnlyArray<EventOption>(new EventOption[2]
		{
			new EventOption(this, TrudgeOn, "DENSE_VEGETATION.pages.INITIAL.options.TRUDGE_ON").ThatDoesDamage(base.DynamicVars.HpLoss.BaseValue),
			new EventOption(this, Rest, "DENSE_VEGETATION.pages.INITIAL.options.REST")
		});
	}

	private void TrudgeOn()
	{
		CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), base.Owner.Creature, base.DynamicVars.HpLoss.BaseValue, ValueProp.Unblockable | ValueProp.Unpowered, null, null);
		PlayerCmd.GainGold(base.DynamicVars.Gold.BaseValue, base.Owner);
		SetEventFinished(L10NLookup("DENSE_VEGETATION.pages.TRUDGE_ON.description"));
	}

	private void Rest()
	{
		PlayerCmd.MimicRestSiteHeal(base.Owner, playSfx: false);
		SetEventState(L10NLookup("DENSE_VEGETATION.pages.REST.description"), new global::_003C_003Ez__ReadOnlySingleElementList<EventOption>(new EventOption(this, Fight, "DENSE_VEGETATION.pages.REST.options.FIGHT")));
	}

	private void Fight()
	{
		EnterCombatWithoutExitingEvent<DenseVegetationEventEncounter>(Array.Empty<Reward>(), shouldResumeAfterCombat: false);
	}
}
