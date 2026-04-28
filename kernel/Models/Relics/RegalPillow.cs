using MegaCrit.Sts2.Core;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rooms;

namespace MegaCrit.Sts2.Core.Models.Relics;

public sealed class RegalPillow : RelicModel
{
	public override RelicRarity Rarity => RelicRarity.Common;

	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlySingleElementList<DynamicVar>(new HealVar(15m));

	public override decimal ModifyRestSiteHealAmount(Creature creature, decimal amount)
	{
		if (creature.Player != base.Owner && creature.PetOwner != base.Owner)
		{
			return amount;
		}
		return amount + base.DynamicVars.Heal.BaseValue;
	}

	public override void AfterRestSiteHeal(Player player, bool isMimicked)
	{
		if (player != base.Owner)
		{
			return;
		}
		 
		base.Status = RelicStatus.Normal;
		return;
	}


	public override void AfterRoomEntered(AbstractRoom room)
	{
		base.Status = ((room is RestSiteRoom) ? RelicStatus.Active : RelicStatus.Normal);
		return;
	}
}