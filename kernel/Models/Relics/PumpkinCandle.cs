using MegaCrit.Sts2.Core;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace MegaCrit.Sts2.Core.Models.Relics;

public sealed class PumpkinCandle : RelicModel
{
	private int _activeAct = -1;

	public override RelicRarity Rarity => RelicRarity.Ancient;

	public override bool HasUponPickupEffect => true;

	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlySingleElementList<DynamicVar>(new EnergyVar(1));


	[SavedProperty]
	public int ActiveAct
	{
		get
		{
			return _activeAct;
		}
		set
		{
			AssertMutable();
			_activeAct = value;
		}
	}

	public override void AfterObtained()
	{
		ActiveAct = base.Owner.RunState.CurrentActIndex;
		return;
	}

	public override decimal ModifyMaxEnergy(Player player, decimal amount)
	{
		if (player != base.Owner)
		{
			return amount;
		}
		if (ActiveAct != base.Owner.RunState.CurrentActIndex)
		{
			return amount;
		}
		return amount + (decimal)base.DynamicVars.Energy.IntValue;
	}

	public override void AfterRoomEntered(AbstractRoom _)
	{
		base.Status = ((ActiveAct != base.Owner.RunState.CurrentActIndex) ? RelicStatus.Disabled : RelicStatus.Normal);
		return;
	}
}