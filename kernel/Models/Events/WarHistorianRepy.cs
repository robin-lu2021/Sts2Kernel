using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;

namespace MegaCrit.Sts2.Core.Models.Events;

public sealed class WarHistorianRepy : EventModel
{
	public override bool IsShared => true;

	public override bool IsAllowed(IRunState runState)
	{
		return false;
	}

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		return new global::_003C_003Ez__ReadOnlyArray<EventOption>(new EventOption[2]
		{
			new EventOption(this, UnlockCage, "WAR_HISTORIAN_REPY.pages.INITIAL.options.UNLOCK_CAGE", HoverTipFactory.FromRelic<HistoryCourse>().Concat(HoverTipFactory.FromCardWithCardHoverTips<LanternKey>())),
			new EventOption(this, UnlockChest, "WAR_HISTORIAN_REPY.pages.INITIAL.options.UNLOCK_CHEST", HoverTipFactory.FromCardWithCardHoverTips<LanternKey>())
		});
	}

	private void UnlockCage()
	{
		SetEventFinished(L10NLookup("WAR_HISTORIAN_REPY.pages.UNLOCK_CAGE.description"));
		base.Owner.RunState.ExtraFields.FreedRepy = true;
		RemoveLanternKey();
		RelicCmd.Obtain(KernelModelDb.Relic<HistoryCourse>().ToMutable(), base.Owner);
	}

	private void UnlockChest()
	{
		SetEventFinished(L10NLookup("WAR_HISTORIAN_REPY.pages.UNLOCK_CHEST.description"));
		RemoveLanternKey();
		List<Reward> list = new List<Reward>();
		list.Add(new PotionReward(base.Owner));
		list.Add(new PotionReward(base.Owner));
		list.Add(new RelicReward(base.Owner));
		list.Add(new RelicReward(base.Owner));
		RewardsCmd.OfferCustom(base.Owner, list);
	}

	private void RemoveLanternKey()
	{
		List<CardModel> list = base.Owner.Deck.Cards.Where((CardModel c) => c is LanternKey).ToList();
		foreach (CardModel item in list)
		{
			PlayerCmd.CompleteQuest(item);
			CardPileCmd.RemoveFromDeck(item);
		}
	}
}

