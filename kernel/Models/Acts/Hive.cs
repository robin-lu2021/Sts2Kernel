using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Unlocks;

namespace MegaCrit.Sts2.Core.Models.Acts;

public sealed class Hive : ActModel
{
	public override IEnumerable<EncounterModel> BossDiscoveryOrder => new global::_003C_003Ez__ReadOnlyArray<EncounterModel>(new EncounterModel[3]
	{
		ModelDb.Encounter<TheInsatiableBoss>(),
		ModelDb.Encounter<KnowledgeDemonBoss>(),
		ModelDb.Encounter<KaiserCrabBoss>()
	});

	public override IEnumerable<AncientEventModel> AllAncients => new global::_003C_003Ez__ReadOnlyArray<AncientEventModel>(new AncientEventModel[3]
	{
		ModelDb.AncientEvent<Orobas>(),
		ModelDb.AncientEvent<Pael>(),
		ModelDb.AncientEvent<Tezcatara>()
	});

	public override IEnumerable<EventModel> AllEvents => new global::_003C_003Ez__ReadOnlyArray<EventModel>(new EventModel[10]
	{
		ModelDb.Event<Amalgamator>(),
		ModelDb.Event<Bugslayer>(),
		ModelDb.Event<ColorfulPhilosophers>(),
		ModelDb.Event<ColossalFlower>(),
		ModelDb.Event<FieldOfManSizedHoles>(),
		ModelDb.Event<InfestedAutomaton>(),
		ModelDb.Event<LostWisp>(),
		ModelDb.Event<SpiritGrafter>(),
		ModelDb.Event<TheLanternKey>(),
		ModelDb.Event<ZenWeaver>()
	});

	protected override int NumberOfWeakEncounters => 2;

	protected override int BaseNumberOfRooms => 14;

	public override string ChestSpineSkinNameNormal => "act2";

	public override string ChestSpineSkinNameStroke => "act2_stroke";

	public override IEnumerable<EncounterModel> GenerateAllEncounters()
	{
		return new global::_003C_003Ez__ReadOnlyArray<EncounterModel>(new EncounterModel[20]
		{
			ModelDb.Encounter<BowlbugsNormal>(),
			ModelDb.Encounter<BowlbugsWeak>(),
			ModelDb.Encounter<ChompersNormal>(),
			ModelDb.Encounter<DecimillipedeElite>(),
			ModelDb.Encounter<EntomancerElite>(),
			ModelDb.Encounter<ExoskeletonsNormal>(),
			ModelDb.Encounter<ExoskeletonsWeak>(),
			ModelDb.Encounter<HunterKillerNormal>(),
			ModelDb.Encounter<KaiserCrabBoss>(),
			ModelDb.Encounter<InfestedPrismsElite>(),
			ModelDb.Encounter<KnowledgeDemonBoss>(),
			ModelDb.Encounter<LouseProgenitorNormal>(),
			ModelDb.Encounter<MytesNormal>(),
			ModelDb.Encounter<OvicopterNormal>(),
			ModelDb.Encounter<SlumberingBeetleNormal>(),
			ModelDb.Encounter<SpinyToadNormal>(),
			ModelDb.Encounter<TheInsatiableBoss>(),
			ModelDb.Encounter<TheObscuraNormal>(),
			ModelDb.Encounter<ThievingHopperWeak>(),
			ModelDb.Encounter<TunnelerWeak>()
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
		int restCount = mapRng.NextGaussianInt(6, 1, 6, 7);
		int unknownCount = MapPointTypeCounts.StandardRandomUnknownCount(mapRng) - 1;
		return new MapPointTypeCounts(unknownCount, restCount);
	}
}
