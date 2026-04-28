using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Unlocks;

namespace MegaCrit.Sts2.Core.Models.Acts;

public sealed class DeprecatedAct : ActModel
{
	public override IEnumerable<EncounterModel> BossDiscoveryOrder => Array.Empty<EncounterModel>();

	public override IEnumerable<AncientEventModel> AllAncients => Array.Empty<AncientEventModel>();

	public override IEnumerable<EventModel> AllEvents => Array.Empty<EventModel>();

	protected override int NumberOfWeakEncounters => 0;

	protected override int BaseNumberOfRooms => 0;

	public override string ChestSpineSkinNameNormal => "";

	public override string ChestSpineSkinNameStroke => "";

	public override IEnumerable<EncounterModel> GenerateAllEncounters()
	{
		return Array.Empty<EncounterModel>();
	}

	public override IEnumerable<AncientEventModel> GetUnlockedAncients(UnlockState unlockState)
	{
		return Array.Empty<AncientEventModel>();
	}

	protected override void ApplyActDiscoveryOrderModifications(UnlockState unlockState)
	{
	}

	public override MapPointTypeCounts GetMapPointTypes(Rng mapRng)
	{
		return new MapPointTypeCounts(0, 0);
	}
}
