using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Unlocks;

namespace MegaCrit.Sts2.Core.Models.Acts;

public sealed class Underdocks : ActModel
{
	public override IEnumerable<EncounterModel> BossDiscoveryOrder => new global::_003C_003Ez__ReadOnlyArray<EncounterModel>(new EncounterModel[3]
	{
		ModelDb.Encounter<WaterfallGiantBoss>(),
		ModelDb.Encounter<SoulFyshBoss>(),
		ModelDb.Encounter<LagavulinMatriarchBoss>()
	});

	public override IEnumerable<AncientEventModel> AllAncients => new global::_003C_003Ez__ReadOnlySingleElementList<AncientEventModel>(ModelDb.AncientEvent<Neow>());

	public override IEnumerable<EventModel> AllEvents => new global::_003C_003Ez__ReadOnlyArray<EventModel>(new EventModel[10]
	{
		ModelDb.Event<AbyssalBaths>(),
		ModelDb.Event<DrowningBeacon>(),
		ModelDb.Event<EndlessConveyor>(),
		ModelDb.Event<PunchOff>(),
		ModelDb.Event<SpiralingWhirlpool>(),
		ModelDb.Event<SunkenStatue>(),
		ModelDb.Event<SunkenTreasury>(),
		ModelDb.Event<DoorsOfLightAndDark>(),
		ModelDb.Event<TrashHeap>(),
		ModelDb.Event<WaterloggedScriptorium>()
	});

	protected override int NumberOfWeakEncounters => 3;

	protected override int BaseNumberOfRooms => 15;

	public override string ChestSpineSkinNameNormal => "act1";

	public override string ChestSpineSkinNameStroke => "act1_stroke";

	public override IEnumerable<EncounterModel> GenerateAllEncounters()
	{
		return new global::_003C_003Ez__ReadOnlyArray<EncounterModel>(new EncounterModel[20]
		{
			ModelDb.Encounter<CorpseSlugsNormal>(),
			ModelDb.Encounter<CorpseSlugsWeak>(),
			ModelDb.Encounter<CultistsNormal>(),
			ModelDb.Encounter<FossilStalkerNormal>(),
			ModelDb.Encounter<GremlinMercNormal>(),
			ModelDb.Encounter<HauntedShipNormal>(),
			ModelDb.Encounter<LagavulinMatriarchBoss>(),
			ModelDb.Encounter<LivingFogNormal>(),
			ModelDb.Encounter<PhantasmalGardenersElite>(),
			ModelDb.Encounter<PunchConstructNormal>(),
			ModelDb.Encounter<SeapunkNormal>(),
			ModelDb.Encounter<SeapunkWeak>(),
			ModelDb.Encounter<SewerClamNormal>(),
			ModelDb.Encounter<SkulkingColonyElite>(),
			ModelDb.Encounter<SludgeSpinnerWeak>(),
			ModelDb.Encounter<SoulFyshBoss>(),
			ModelDb.Encounter<TerrorEelElite>(),
			ModelDb.Encounter<ToadpolesWeak>(),
			ModelDb.Encounter<TwoTailedRatsNormal>(),
			ModelDb.Encounter<WaterfallGiantBoss>()
		});
	}

	public override IEnumerable<AncientEventModel> GetUnlockedAncients(UnlockState unlockState)
	{
		return AllAncients.ToList();
	}

	protected override void ApplyActDiscoveryOrderModifications(UnlockState unlockState)
	{
	}

	public override MapPointTypeCounts GetMapPointTypes(Rng mapRng)
	{
		int restCount = mapRng.NextGaussianInt(7, 1, 6, 7);
		int unknownCount = MapPointTypeCounts.StandardRandomUnknownCount(mapRng);
		return new MapPointTypeCounts(unknownCount, restCount);
	}
}
