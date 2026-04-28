using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Unlocks;

namespace MegaCrit.Sts2.Core.Models.RelicPools;

public sealed class IroncladRelicPool : RelicPoolModel
{
	public override string EnergyColorName => "ironclad";

	protected override IEnumerable<RelicModel> GenerateAllRelics()
	{
		return new global::_003C_003Ez__ReadOnlyArray<RelicModel>(new RelicModel[8]
		{
			ModelDb.Relic<Brimstone>(),
			ModelDb.Relic<BurningBlood>(),
			ModelDb.Relic<CharonsAshes>(),
			ModelDb.Relic<DemonTongue>(),
			ModelDb.Relic<PaperPhrog>(),
			ModelDb.Relic<RedSkull>(),
			ModelDb.Relic<RuinedHelmet>(),
			ModelDb.Relic<SelfFormingClay>()
		});
	}

	public override IEnumerable<RelicModel> GetUnlockedRelics(UnlockState unlockState)
	{
		return base.AllRelics.ToList();
	}
}
