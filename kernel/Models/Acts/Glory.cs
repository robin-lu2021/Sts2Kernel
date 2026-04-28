using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Unlocks;

namespace MegaCrit.Sts2.Core.Models.Acts;

public sealed class Glory : ActModel
{
	public override IEnumerable<EncounterModel> BossDiscoveryOrder => new global::_003C_003Ez__ReadOnlyArray<EncounterModel>(new EncounterModel[3]
	{
		ModelDb.Encounter<QueenBoss>(),
		ModelDb.Encounter<TestSubjectBoss>(),
		ModelDb.Encounter<DoormakerBoss>()
	});

	public override IEnumerable<AncientEventModel> AllAncients => new global::_003C_003Ez__ReadOnlyArray<AncientEventModel>(new AncientEventModel[3]
	{
		ModelDb.AncientEvent<Nonupeipe>(),
		ModelDb.AncientEvent<Tanx>(),
		ModelDb.AncientEvent<Vakuu>()
	});

	public override IEnumerable<EventModel> AllEvents => new global::_003C_003Ez__ReadOnlyArray<EventModel>(new EventModel[7]
	{
		ModelDb.Event<BattlewornDummy>(),
		ModelDb.Event<GraveOfTheForgotten>(),
		ModelDb.Event<HungryForMushrooms>(),
		ModelDb.Event<Reflections>(),
		ModelDb.Event<RoundTeaParty>(),
		ModelDb.Event<Trial>(),
		ModelDb.Event<TinkerTime>()
	});

	protected override int NumberOfWeakEncounters => 2;

	protected override int BaseNumberOfRooms => 13;

	public override string ChestSpineSkinNameNormal => "act3";

	public override string ChestSpineSkinNameStroke => "act3_stroke";

	public override IEnumerable<EncounterModel> GenerateAllEncounters()
	{
		return new global::_003C_003Ez__ReadOnlyArray<EncounterModel>(new EncounterModel[18]
		{
			ModelDb.Encounter<AxebotsNormal>(),
			ModelDb.Encounter<ConstructMenagerieNormal>(),
			ModelDb.Encounter<DevotedSculptorWeak>(),
			ModelDb.Encounter<DoormakerBoss>(),
			ModelDb.Encounter<FabricatorNormal>(),
			ModelDb.Encounter<FrogKnightNormal>(),
			ModelDb.Encounter<GlobeHeadNormal>(),
			ModelDb.Encounter<KnightsElite>(),
			ModelDb.Encounter<MechaKnightElite>(),
			ModelDb.Encounter<OwlMagistrateNormal>(),
			ModelDb.Encounter<QueenBoss>(),
			ModelDb.Encounter<ScrollsOfBitingNormal>(),
			ModelDb.Encounter<ScrollsOfBitingWeak>(),
			ModelDb.Encounter<SlimedBerserkerNormal>(),
			ModelDb.Encounter<SoulNexusElite>(),
			ModelDb.Encounter<TestSubjectBoss>(),
			ModelDb.Encounter<TheLostAndForgottenNormal>(),
			ModelDb.Encounter<TurretOperatorWeak>()
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
		int restCount = mapRng.NextInt(5, 7);
		int unknownCount = MapPointTypeCounts.StandardRandomUnknownCount(mapRng) - 1;
		return new MapPointTypeCounts(unknownCount, restCount);
	}
}
