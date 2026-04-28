using MegaCrit.Sts2.Core;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace MegaCrit.Sts2.Core.Models.Relics;

public sealed class Girya : RelicModel
{
	private int _timesLifted;

	public const int maxLifts = 3;

	public override RelicRarity Rarity => RelicRarity.Rare;



	[SavedProperty]
	public int TimesLifted
	{
		get
		{
			return _timesLifted;
		}
		set
		{
			AssertMutable();
			_timesLifted = value;
			InvokeDisplayAmountChanged();
		}
	}


	public override bool IsAllowed(IRunState runState)
	{
		return RelicModel.IsBeforeAct3TreasureChest(runState);
	}

	public override void AfterRoomEntered(AbstractRoom room)
	{
		if (TimesLifted > 0 && room is CombatRoom)
		{
			 
			PowerCmd.Apply<StrengthPower>(base.Owner.Creature, TimesLifted, base.Owner.Creature, null);
		}
	}

	public override bool TryModifyRestSiteOptions(Player player, ICollection<RestSiteOption> options)
	{
		if (player != base.Owner)
		{
			return false;
		}
		if (TimesLifted >= 3)
		{
			return false;
		}
		options.Add(new LiftRestSiteOption(player));
		return true;
	}
}