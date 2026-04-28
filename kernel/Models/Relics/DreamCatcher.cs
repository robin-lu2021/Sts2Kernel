using MegaCrit.Sts2.Core;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace MegaCrit.Sts2.Core.Models.Relics;

public sealed class DreamCatcher : RelicModel
{
	public override RelicRarity Rarity => RelicRarity.Event;

	public override bool TryModifyRestSiteHealRewards(Player player, List<Reward> rewards, bool isMimicked)
	{
		if (player != base.Owner)
		{
			return false;
		}
		rewards.Add(new CardReward(CardCreationOptions.ForRoom(player, RoomType.Monster), 3, base.Owner));
		 
		return true;
	}

}