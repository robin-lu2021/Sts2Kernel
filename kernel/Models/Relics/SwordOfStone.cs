using MegaCrit.Sts2.Core;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace MegaCrit.Sts2.Core.Models.Relics;

public sealed class SwordOfStone : RelicModel
{
	private const string _elitesKey = "Elites";

	private int _elitesDefeated;

	public override RelicRarity Rarity => RelicRarity.Event;



	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlySingleElementList<DynamicVar>(new DynamicVar("Elites", 5m));

	[SavedProperty]
	public int ElitesDefeated
	{
		get
		{
			return _elitesDefeated;
		}
		set
		{
			AssertMutable();
			_elitesDefeated = value;
			InvokeDisplayAmountChanged();
		}
	}

	public override void AfterCombatVictory(CombatRoom room)
	{
		if (room.RoomType == RoomType.Elite)
		{
			ElitesDefeated++;
			 
			if ((decimal)ElitesDefeated >= base.DynamicVars["Elites"].BaseValue)
			{
				RelicCmd.Replace(this, KernelModelDb.Relic<SwordOfJade>().ToMutable());
			}
		}
	}
}
