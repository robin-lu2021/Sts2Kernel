using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Unlocks;

namespace MegaCrit.Sts2.Core.Models.RelicPools;

public sealed class DefectRelicPool : RelicPoolModel
{
	public override string EnergyColorName => "defect";

	protected override IEnumerable<RelicModel> GenerateAllRelics()
	{
		return new global::_003C_003Ez__ReadOnlyArray<RelicModel>(new RelicModel[8]
		{
			ModelDb.Relic<CrackedCore>(),
			ModelDb.Relic<DataDisk>(),
			ModelDb.Relic<EmotionChip>(),
			ModelDb.Relic<GoldPlatedCables>(),
			ModelDb.Relic<PowerCell>(),
			ModelDb.Relic<Metronome>(),
			ModelDb.Relic<RunicCapacitor>(),
			ModelDb.Relic<SymbioticVirus>()
		});
	}

	public override IEnumerable<RelicModel> GetUnlockedRelics(UnlockState unlockState)
	{
		return base.AllRelics.ToList();
	}
}
