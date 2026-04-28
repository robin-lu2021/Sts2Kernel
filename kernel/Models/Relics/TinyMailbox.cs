using MegaCrit.Sts2.Core;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Rewards;

namespace MegaCrit.Sts2.Core.Models.Relics;

public sealed class TinyMailbox : RelicModel
{
	public override RelicRarity Rarity => RelicRarity.Uncommon;

	public override bool TryModifyRestSiteHealRewards(Player player, List<Reward> rewards, bool isMimicked)
	{
		if (player != base.Owner)
		{
			return false;
		}
		rewards.Add(new PotionReward(player));
		rewards.Add(new PotionReward(player));
		 
		return true;
	}

}