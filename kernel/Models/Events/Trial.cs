using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;

namespace MegaCrit.Sts2.Core.Models.Events;

public sealed class Trial : EventModel
{
	private const string _entrantNumberKey = "EntrantNumber";

	private const string _trialResultKey = "TrialResult";

	private const string _trialStoryKey = "TrialStory";

	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlySingleElementList<DynamicVar>(new DynamicVar("EntrantNumber", -1m));

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		return new global::_003C_003Ez__ReadOnlyArray<EventOption>(new EventOption[2]
		{
			new EventOption(this, Accept, "TRIAL.pages.INITIAL.options.ACCEPT"),
			new EventOption(this, Reject, "TRIAL.pages.INITIAL.options.REJECT")
		});
	}

	private void Accept()
	{
		string entryName;
		EventOption[] eventOptions;
		switch (base.Rng.NextInt(3))
		{
		case 0:
			entryName = "TRIAL.pages.MERCHANT.description";
			eventOptions = new EventOption[2]
			{
				new EventOption(this, MerchantGuilty, "TRIAL.pages.MERCHANT.options.GUILTY", KernelHoverTipFactory.FromCardWithCardHoverTips<Regret>()),
				new EventOption(this, MerchantInnocent, "TRIAL.pages.MERCHANT.options.INNOCENT", KernelHoverTipFactory.FromCardWithCardHoverTips<Shame>())
			};
			break;
		case 1:
			entryName = "TRIAL.pages.NOBLE.description";
			eventOptions = new EventOption[2]
			{
				new EventOption(this, NobleGuilty, "TRIAL.pages.NOBLE.options.GUILTY"),
				new EventOption(this, NobleInnocent, "TRIAL.pages.NOBLE.options.INNOCENT", KernelHoverTipFactory.FromCardWithCardHoverTips<Regret>())
			};
			break;
		case 2:
			entryName = "TRIAL.pages.NONDESCRIPT.description";
			eventOptions = new EventOption[2]
			{
				new EventOption(this, NondescriptGuilty, "TRIAL.pages.NONDESCRIPT.options.GUILTY", KernelHoverTipFactory.FromCardWithCardHoverTips<Doubt>()),
				new EventOption(this, NondescriptInnocent, "TRIAL.pages.NONDESCRIPT.options.INNOCENT", KernelHoverTipFactory.FromCardWithCardHoverTips<Doubt>().Concat(new global::_003C_003Ez__ReadOnlySingleElementList<IHoverTip>(HoverTipFactory.Static(StaticHoverTip.Transform))))
			};
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}
		LocString locString = L10NLookup("TRIAL.trialFormat");
		locString.Add(new StringVar("TrialStory", L10NLookup(entryName).GetRawText()));
		SetEventState(locString, eventOptions);
	}

	private void Reject()
	{
		EventOption[] eventOptions = new EventOption[2]
		{
			new EventOption(this, Accept, "TRIAL.pages.REJECT.options.ACCEPT"),
			new EventOption(this, DoubleDown, "TRIAL.pages.REJECT.options.DOUBLE_DOWN", false, true).ThatWillKillPlayerIf((Player _) => true)
		};
		LocString description = L10NLookup("TRIAL.pages.REJECT.description");
		SetEventState(description, eventOptions);
	}

	private void DoubleDown()
	{
		CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), base.Owner.Creature, 9999m, ValueProp.Unblockable | ValueProp.Unpowered, null, null);
		if (base.Owner.Creature.IsDead)
		{
			SetEventFinished(L10NLookup("GENERIC.youAreDead.description"));
			return;
		}
		SetEventFinished(L10NLookup("TRIAL.pages.REJECT.description"));
	}

	private void MerchantGuilty()
	{
		CardPileCmd.AddCurseToDeck<Regret>(base.Owner);
		for (int i = 0; i < 2; i++)
		{
			RelicCmd.Obtain(RelicFactory.PullNextRelicFromFront(base.Owner).ToMutable(), base.Owner);
		}
		SetTrialFinished("TRIAL.pages.MERCHANT_GUILTY.description");
	}

	private void MerchantInnocent()
	{
		CardPileCmd.AddCurseToDeck<Shame>(base.Owner);
		foreach (CardModel item in RunSynchronously(CardSelectCmd.FromDeckForUpgrade(prefs: new CardSelectorPrefs(CardSelectorPrefs.UpgradeSelectionPrompt, 2), player: base.Owner)))
		{
			CardCmd.Upgrade(item);
		}
		SetTrialFinished("TRIAL.pages.MERCHANT_INNOCENT.description");
	}

	private void NobleGuilty()
	{
		CreatureCmd.Heal(base.Owner.Creature, 10m);
		SetTrialFinished("TRIAL.pages.NOBLE_GUILTY.description");
	}

	private void NobleInnocent()
	{
		CardPileCmd.AddCurseToDeck<Regret>(base.Owner);
		PlayerCmd.GainGold(300m, base.Owner);
		SetTrialFinished("TRIAL.pages.NOBLE_INNOCENT.description");
	}

	private void NondescriptGuilty()
	{
		CardPileCmd.AddCurseToDeck<Doubt>(base.Owner);
		List<Reward> list = new List<Reward>();
		for (int i = 0; i < 2; i++)
		{
			list.Add(new CardReward(CardCreationOptions.ForNonCombatWithDefaultOdds(new global::_003C_003Ez__ReadOnlySingleElementList<CardPoolModel>(base.Owner.Character.CardPool)), 3, base.Owner));
		}
		RewardsCmd.OfferCustom(base.Owner, list);
		SetTrialFinished("TRIAL.pages.NONDESCRIPT_GUILTY.description");
	}

	private void NondescriptInnocent()
	{
		CardPileCmd.AddCurseToDeck<Doubt>(base.Owner);
		List<CardModel> list = RunSynchronously(CardSelectCmd.FromDeckForTransformation(prefs: new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, 2), player: base.Owner).ToList());
		foreach (CardModel item in list)
		{
			CardCmd.TransformToRandom(item, base.Owner.RunState.Rng.Niche, CardPreviewStyle.None);
		}
		SetTrialFinished("TRIAL.pages.NONDESCRIPT_INNOCENT.description");
	}

	private void SetTrialFinished(string trialResultLoc)
	{
		LocString locString = L10NLookup("TRIAL.trialResult");
		locString.Add(new StringVar("TrialResult", L10NLookup(trialResultLoc).GetRawText()));
		SetEventFinished(locString);
	}

	public override void CalculateVars()
	{
		if (base.DynamicVars["EntrantNumber"].BaseValue == -1m)
		{
			base.DynamicVars["EntrantNumber"].BaseValue = MegaCrit.Sts2.Core.Random.Rng.Chaotic.NextInt(101, 999);
		}
	}
}
