using MegaCrit.Sts2.Core;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace MegaCrit.Sts2.Core.Models.Relics;

public sealed class WongosMysteryTicket : RelicModel
{
	private const string _remainingCombatsKey = "RemainingCombats";

	public const int combatsToActivate = 5;

	public const int relicCount = 3;

	private int _combatsFinished;

	private bool _gaveRelic;

	public override RelicRarity Rarity => RelicRarity.Event;

	public override bool IsUsedUp => GaveRelic;



	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlyArray<DynamicVar>(new DynamicVar[2]
	{
		new RepeatVar(3),
		new DynamicVar("RemainingCombats", 5m)
	});

	[SavedProperty]
	public int CombatsFinished
	{
		get
		{
			return _combatsFinished;
		}
		set
		{
			AssertMutable();
			_combatsFinished = value;
			InvokeDisplayAmountChanged();
		}
	}

	[SavedProperty]
	public bool GaveRelic
	{
		get
		{
			return _gaveRelic;
		}
		set
		{
			AssertMutable();
			_gaveRelic = value;
			InvokeDisplayAmountChanged();
			if (_gaveRelic)
			{
				base.Status = RelicStatus.Disabled;
			}
		}
	}

	public override void AfterCombatEnd(CombatRoom _)
	{
		CombatsFinished++;
		int num = 5 - CombatsFinished;
		base.DynamicVars["RemainingCombats"].BaseValue = decimal.Max(num, 0m);
		return;
	}

	public override bool TryModifyRewards(Player player, List<Reward> rewards, AbstractRoom? room)
	{
		if (player != base.Owner)
		{
			return false;
		}
		if (!(room is CombatRoom))
		{
			return false;
		}
		if (GaveRelic)
		{
			return false;
		}
		int num = 5 - CombatsFinished;
		if (num > 0)
		{
			return false;
		}
		for (int i = 0; i < base.DynamicVars.Repeat.IntValue; i++)
		{
			rewards.Add(new RelicReward(player));
		}
		return true;
	}

	public override void AfterModifyingRewards()
	{
		 
		GaveRelic = true;
		return;
	}
}