using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Unlocks;

namespace MegaCrit.Sts2.Core.Models.RelicPools;

public sealed class RegentRelicPool : RelicPoolModel
{
	public override string EnergyColorName => "regent";

	protected override IEnumerable<RelicModel> GenerateAllRelics()
	{
		return new global::_003C_003Ez__ReadOnlyArray<RelicModel>(new RelicModel[8]
		{
			ModelDb.Relic<DivineRight>(),
			ModelDb.Relic<FencingManual>(),
			ModelDb.Relic<GalacticDust>(),
			ModelDb.Relic<LunarPastry>(),
			ModelDb.Relic<MiniRegent>(),
			ModelDb.Relic<OrangeDough>(),
			ModelDb.Relic<Regalite>(),
			ModelDb.Relic<VitruvianMinion>()
		});
	}

	public override IEnumerable<RelicModel> GetUnlockedRelics(UnlockState unlockState)
	{
		return base.AllRelics.ToList();
	}
}
