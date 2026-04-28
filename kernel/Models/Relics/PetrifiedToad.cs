using MegaCrit.Sts2.Core;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Potions;

namespace MegaCrit.Sts2.Core.Models.Relics;

public sealed class PetrifiedToad : RelicModel
{
	public override RelicRarity Rarity => RelicRarity.Uncommon;


	public override void BeforeCombatStartLate()
	{
		 
		PotionCmd.TryToProcure<PotionShapedRock>(base.Owner);
	}
}