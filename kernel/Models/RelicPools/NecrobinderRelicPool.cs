using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Unlocks;

namespace MegaCrit.Sts2.Core.Models.RelicPools;

public sealed class NecrobinderRelicPool : RelicPoolModel
{
	public override string EnergyColorName => "necrobinder";

	protected override IEnumerable<RelicModel> GenerateAllRelics()
	{
		return new global::_003C_003Ez__ReadOnlyArray<RelicModel>(new RelicModel[8]
		{
			ModelDb.Relic<BigHat>(),
			ModelDb.Relic<BoneFlute>(),
			ModelDb.Relic<BookRepairKnife>(),
			ModelDb.Relic<Bookmark>(),
			ModelDb.Relic<BoundPhylactery>(),
			ModelDb.Relic<FuneraryMask>(),
			ModelDb.Relic<IvoryTile>(),
			ModelDb.Relic<UndyingSigil>()
		});
	}

	public override IEnumerable<RelicModel> GetUnlockedRelics(UnlockState unlockState)
	{
		return base.AllRelics.ToList();
	}
}
