using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;

namespace MegaCrit.Sts2.Core.Models.Events;

public sealed class PunchOff : EventModel
{
	public override EncounterModel CanonicalEncounter => ModelDb.Encounter<PunchOffEventEncounter>();

	public override bool IsShared => true;

	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlySingleElementList<DynamicVar>(new GoldVar(0));

	public override bool IsAllowed(IRunState runState)
	{
		return runState.TotalFloor >= 6;
	}

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		return new global::_003C_003Ez__ReadOnlyArray<EventOption>(new EventOption[2]
		{
			new EventOption(this, Nab, "PUNCH_OFF.pages.INITIAL.options.NAB", KernelHoverTipFactory.FromCardWithCardHoverTips<Injury>()),
			new EventOption(this, TakeThem, "PUNCH_OFF.pages.INITIAL.options.I_CAN_TAKE_THEM")
		});
	}

	public override void AfterEventStarted()
	{
		base.Owner.CanRemovePotions = false;
		return;
	}

	public override void CalculateVars()
	{
		base.DynamicVars.Gold.BaseValue = base.Rng.NextInt(91, 99);
	}

	protected override void OnEventFinished()
	{
		base.Owner.CanRemovePotions = true;
	}

	private void Nab()
	{
		CardPileCmd.AddCurseToDeck<Injury>(base.Owner);
		RewardsCmd.OfferCustom(base.Owner, new List<Reward>(1)
		{
			new RelicReward(base.Owner)
		});
		SetEventFinished(L10NLookup("PUNCH_OFF.pages.NAB.description"));
	}

	private void TakeThem()
	{
		SetEventState(L10NLookup("PUNCH_OFF.pages.I_CAN_TAKE_THEM.description"), new global::_003C_003Ez__ReadOnlySingleElementList<EventOption>(new EventOption(this, Fight, "PUNCH_OFF.pages.I_CAN_TAKE_THEM.options.FIGHT")));
	}

	private void Fight()
	{
		base.Owner.CanRemovePotions = true;
		EnterCombatWithoutExitingEvent<PunchOffEventEncounter>(new global::_003C_003Ez__ReadOnlyArray<Reward>(new Reward[2]
		{
			new RelicReward(base.Owner),
			new PotionReward(base.Owner)
		}), shouldResumeAfterCombat: false);
	}
}
